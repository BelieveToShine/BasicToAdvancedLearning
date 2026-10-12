#!/usr/bin/env bash
# Smoke test for FullStackBank.Api: calls EVERY endpoint with curl and exits non-zero if any status (or
# a key part of the answer) is not what the API promises.
#
# Usage:   ./smoke.sh [base-url]        (default http://localhost:5080, or set BASE_URL)
# Needs:   the API running (see README.md), bash and curl. No jq needed.
# Re-runnable: it makes up a unique account number each run. Each run leaves ONE extra account behind (the one
# that gets transactions: an account with history cannot be deleted - that is exactly what step 11 checks).

BASE="${1:-${BASE_URL:-http://localhost:5080}}"
FAILS=0
STEPS=0
STATUS=""
HEADERS=""
BODY=""

# call METHOD PATH [JSON-BODY]  -> fills STATUS, HEADERS, BODY
call() {
  local method="$1" path="$2" data="${3-}"
  local args=(-s -D - -X "$method" "$BASE$path" -H 'Accept: application/json')
  # data "__EMPTY__" = send an empty body WITH Content-Type: application/json.
  if [ "$data" = "__EMPTY__" ]; then data=""; args+=(-H 'Content-Type: application/json' --data-binary ''); fi
  if [ -n "$data" ]; then
    # NO_CT=1: send a body WITHOUT a Content-Type header (used to check the 415 answer).
    if [ -z "${NO_CT-}" ]; then args+=(-H 'Content-Type: application/json'); fi
    args+=(-d "$data")
  fi
  local raw
  raw="$(curl "${args[@]}" -w '\n__CODE__%{http_code}')" || { echo "FAIL: cannot reach $BASE (is the API running?)"; exit 2; }
  STATUS="${raw##*__CODE__}"
  raw="${raw%$'\n'__CODE__*}"
  HEADERS="${raw%%$'\r\n\r\n'*}"
  BODY="${raw#*$'\r\n\r\n'}"
}

# expect "description" METHOD PATH EXPECTED_STATUS [JSON-BODY] [TEXT THE BODY MUST CONTAIN]
expect() {
  local what="$1" method="$2" path="$3" want="$4" data="${5-}" must="${6-}"
  STEPS=$((STEPS + 1))
  call "$method" "$path" "$data"
  local ok=1
  [ "$STATUS" = "$want" ] || ok=0
  if [ -n "$must" ] && [[ "$BODY" != *"$must"* ]]; then ok=0; fi
  # Every error answer must be ProblemDetails, and must never leak a stack trace.
  if [ "$want" -ge 400 ] 2>/dev/null; then
    [[ "$HEADERS" == *"application/problem+json"* ]] || ok=0
    [[ "$BODY" != *"StackTrace"* && "$BODY" != *" at FullStackBank"* ]] || ok=0
  fi
  if [ $ok = 1 ]; then
    printf 'ok    %-52s %s %s -> %s\n' "$what" "$method" "$path" "$STATUS"
  else
    printf 'FAIL  %-52s %s %s -> %s (wanted %s%s)\n      body: %s\n' "$what" "$method" "$path" "$STATUS" "$want" "${must:+, body containing \"$must\"}" "$BODY"
    FAILS=$((FAILS + 1))
  fi
}

# json_number NAME -> value of the first "NAME":<number> in BODY
json_number() { printf '%s' "$BODY" | grep -o "\"$1\":[0-9.]*" | head -1 | cut -d: -f2; }

# Max 20 characters (the API enforces it), and the "-B" suffix used below must fit too.
# keys_are "description" "key,key,..." -> the JSON object in BODY has exactly these property names (contract check)
keys_are() {
  STEPS=$((STEPS + 1))
  local got; got="$(printf '%s' "$BODY" | grep -o '"[A-Za-z]*":' | tr -d '":' | paste -sd, -)"
  if [ "$got" = "$2" ]; then printf 'ok    %-52s keys: %s\n' "$1" "$got"
  else printf 'FAIL  %-52s keys: %s (wanted %s)\n' "$1" "$got" "$2"; FAILS=$((FAILS + 1)); fi
}

NUMBER="S$(date +%s)$RANDOM"
echo "Smoke test against $BASE (account number $NUMBER)"

expect  "1  health"                              GET    /health                          200 "" "Healthy"
expect  "2  list accounts"                       GET    /api/accounts                    200 "" "ACC-1001"
expect  "3a list filtered by type"               GET    "/api/accounts?type=Savings"     200 "" '"accountType":"Savings"'
expect  "3b list with an unknown type"           GET    "/api/accounts?type=Gold"        400 "" '"type"'
expect  "4a get one account"                     GET    /api/accounts/1                  200 "" '"accountNumber":"ACC-1001"'
keys_are "4a account JSON has exactly the contract fields" "accountId,customerName,accountNumber,accountType,balance"
expect  "4b get an unknown account"              GET    /api/accounts/999999             404
expect  "5a create account"                      POST   /api/accounts                    201 "{\"customerId\":1,\"accountNumber\":\"$NUMBER\",\"accountType\":\"Current\"}" '"balance":0'
[[ "$HEADERS" == *"Location: "*"/api/accounts/"* ]] || { echo "FAIL  5a response has no Location header"; FAILS=$((FAILS + 1)); }
ID="$(json_number accountId)"
[ -n "$ID" ] || { echo "FAIL  could not read the new account id from: $BODY"; exit 1; }
expect  "5b duplicate account number"            POST   /api/accounts                    409 "{\"customerId\":1,\"accountNumber\":\"$NUMBER\",\"accountType\":\"Current\"}" "Duplicate"
expect  "5c invalid body (errors keyed by field)" POST  /api/accounts                    400 '{"customerId":1,"accountNumber":"bad number!","accountType":"Gold"}' '"accountNumber":['
expect  "5d unknown customer"                    POST   /api/accounts                    400 '{"customerId":99999,"accountNumber":"ACC-X","accountType":"Savings"}' '"customerId":['
expect  "5e malformed JSON"                      POST   /api/accounts                    400 '{not json'
expect  "6  new account is readable"             GET    "/api/accounts/$ID"              200 "" "\"accountNumber\":\"$NUMBER\""
expect  "7  no transactions yet"                 GET    "/api/accounts/$ID/transactions" 200 "" "[]"
expect  "7b transactions of unknown account"     GET    /api/accounts/999999/transactions 404
expect  "8a withdrawal from an empty account"    POST   "/api/accounts/$ID/transactions" 422 '{"kind":"Withdrawal","amount":10}' "Insufficient funds"
expect  "8b deposit"                             POST   "/api/accounts/$ID/transactions" 201 '{"kind":"Deposit","amount":100.50}' '"amount":100.50'
[[ "$HEADERS" == *"Location: "*"/api/accounts/$ID/transactions/"* ]] || { echo "FAIL  8b response has no Location header"; FAILS=$((FAILS + 1)); }
keys_are "8b transaction JSON has exactly the contract fields" "transactionId,kind,amount,createdAt"
TX="$(json_number transactionId)"
expect  "8c withdrawal within the balance"       POST   "/api/accounts/$ID/transactions" 201 '{"kind":"Withdrawal","amount":40}' '"amount":40.00'
expect  "8d amount 0"                            POST   "/api/accounts/$ID/transactions" 400 '{"kind":"Deposit","amount":0}' '"amount":['
expect  "8e negative amount"                     POST   "/api/accounts/$ID/transactions" 400 '{"kind":"Withdrawal","amount":-5}' '"amount":['
expect  "8d2 amount with 3 decimals"             POST   "/api/accounts/$ID/transactions" 400 '{"kind":"Deposit","amount":10.005}' "at most 2 decimal places"
expect  "8d3 amount over the cap"                POST   "/api/accounts/$ID/transactions" 400 '{"kind":"Deposit","amount":1000000000001}' "between 0.01 and 1,000,000,000,000.00"
expect  "8d4 amount of the wrong JSON type"      POST   "/api/accounts/$ID/transactions" 400 '{"kind":"Deposit","amount":"abc"}' "The value for 'amount' has the wrong type or format."
NO_CT=1 expect "8d5 body without Content-Type"   POST   "/api/accounts/$ID/transactions" 415 '{"kind":"Deposit","amount":5}'
expect  "8d6 empty body"                         POST   "/api/accounts/$ID/transactions" 400 "__EMPTY__" '"body"'
expect  "8f unknown kind"                        POST   "/api/accounts/$ID/transactions" 400 '{"kind":"Transfer","amount":5}' '"kind":['
expect  "8g transaction on unknown account"      POST   /api/accounts/999999/transactions 404 '{"kind":"Deposit","amount":5}'
expect  "9a list transactions (newest first)"    GET    "/api/accounts/$ID/transactions" 200 "" '"kind":"Withdrawal"'
expect  "9b get one transaction"                 GET    "/api/accounts/$ID/transactions/$TX" 200 "" '"transactionId":'
expect  "9c unknown transaction"                 GET    "/api/accounts/$ID/transactions/999999999" 404
expect  "10 balance reflects the transactions"   GET    "/api/accounts/$ID"              200 "" '"balance":60.50'
expect  "11 delete account that has history"     DELETE "/api/accounts/$ID"              409 "" "cannot be deleted"

# A fresh, empty account CAN be deleted.
expect  "12a create an empty account"            POST   /api/accounts                    201 "{\"customerId\":2,\"accountNumber\":\"$NUMBER-B\",\"accountType\":\"Savings\"}"
EMPTY_ID="$(json_number accountId)"
[ -n "$EMPTY_ID" ] || { echo "FAIL  could not read the id of the empty account; stopping"; exit 1; }
expect  "12b delete it"                          DELETE "/api/accounts/$EMPTY_ID"        204
expect  "12c it is gone"                         GET    "/api/accounts/$EMPTY_ID"        404
expect  "12d delete again"                       DELETE "/api/accounts/$EMPTY_ID"        404
expect  "13 unknown URL"                         GET    /api/nothing-here                404

echo
if [ "$FAILS" -eq 0 ]; then
  echo "PASS: $STEPS checks, 0 failures"
  exit 0
fi
echo "FAILED: $FAILS of $STEPS checks (plus any lines marked FAIL above)"
exit 1
