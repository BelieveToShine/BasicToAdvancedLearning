namespace BasicToAdvancedLearning.ApiTesting;

// A copy of the two shipped files (bank-api.postman_collection.json and bank-api.http), kept as text so the
// lesson still works if it cannot find the real files on disk (for example when run from a published folder).
// The lesson prefers the real files and says which one it used. If you edit a file, edit its copy here too
// (the lesson prints whether the two copies match).
internal static class EmbeddedFiles
{
    public const string PostmanCollection = """
{
  "info": {
    "name": "Bank API (Topic 19)",
    "description": "Requests for the in-memory bank API. Set baseUrl to where the API is running. No secrets: this API has no login. These files are for an API you run as its own program; this lesson has no such program, so it replays the same requests against the in-process copy. Opening an account stores its id in newAccountId, and the last request deletes that account, so the collection can be run again against the same running API (the deposit changes account 1's balance each run).",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "List all accounts",
      "event": [
        {
          "listen": "test",
          "script": {
            "type": "text/javascript",
            "exec": [
              "pm.test(\"Status code is 200\", function () {",
              "    pm.response.to.have.status(200);",
              "});"
            ]
          }
        }
      ],
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/api/accounts",
          "host": [
            "{{baseUrl}}"
          ],
          "path": [
            "api",
            "accounts"
          ]
        }
      }
    },
    {
      "name": "Get one account",
      "event": [
        {
          "listen": "test",
          "script": {
            "type": "text/javascript",
            "exec": [
              "pm.test(\"Status code is 200\", function () {",
              "    pm.response.to.have.status(200);",
              "});"
            ]
          }
        }
      ],
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/api/accounts/{{accountId}}",
          "host": [
            "{{baseUrl}}"
          ],
          "path": [
            "api",
            "accounts",
            "{{accountId}}"
          ]
        }
      }
    },
    {
      "name": "Get an account that does not exist",
      "event": [
        {
          "listen": "test",
          "script": {
            "type": "text/javascript",
            "exec": [
              "pm.test(\"Status code is 404\", function () {",
              "    pm.response.to.have.status(404);",
              "});"
            ]
          }
        }
      ],
      "request": {
        "method": "GET",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/api/accounts/999",
          "host": [
            "{{baseUrl}}"
          ],
          "path": [
            "api",
            "accounts",
            "999"
          ]
        }
      }
    },
    {
      "name": "Open a new account",
      "event": [
        {
          "listen": "test",
          "script": {
            "type": "text/javascript",
            "exec": [
              "pm.test(\"Status code is 201\", function () {",
              "    pm.response.to.have.status(201);",
              "});",
              "pm.collectionVariables.set(\"newAccountId\", pm.response.json().id);"
            ]
          }
        }
      ],
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/accounts",
          "host": [
            "{{baseUrl}}"
          ],
          "path": [
            "api",
            "accounts"
          ]
        },
        "body": {
          "mode": "raw",
          "raw": "{\n  \"owner\": \"Chen Li\",\n  \"accountType\": \"Savings\"\n}",
          "options": {
            "raw": {
              "language": "json"
            }
          }
        }
      }
    },
    {
      "name": "Open an account with an invalid type",
      "event": [
        {
          "listen": "test",
          "script": {
            "type": "text/javascript",
            "exec": [
              "pm.test(\"Status code is 400\", function () {",
              "    pm.response.to.have.status(400);",
              "});"
            ]
          }
        }
      ],
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/accounts",
          "host": [
            "{{baseUrl}}"
          ],
          "path": [
            "api",
            "accounts"
          ]
        },
        "body": {
          "mode": "raw",
          "raw": "{\n  \"owner\": \"Chen Li\",\n  \"accountType\": \"Gold\"\n}",
          "options": {
            "raw": {
              "language": "json"
            }
          }
        }
      }
    },
    {
      "name": "Deposit money",
      "event": [
        {
          "listen": "test",
          "script": {
            "type": "text/javascript",
            "exec": [
              "pm.test(\"Status code is 200\", function () {",
              "    pm.response.to.have.status(200);",
              "});"
            ]
          }
        }
      ],
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/accounts/{{accountId}}/deposit",
          "host": [
            "{{baseUrl}}"
          ],
          "path": [
            "api",
            "accounts",
            "{{accountId}}",
            "deposit"
          ]
        },
        "body": {
          "mode": "raw",
          "raw": "{\n  \"amount\": 50.25\n}",
          "options": {
            "raw": {
              "language": "json"
            }
          }
        }
      }
    },
    {
      "name": "Deposit a negative amount",
      "event": [
        {
          "listen": "test",
          "script": {
            "type": "text/javascript",
            "exec": [
              "pm.test(\"Status code is 400\", function () {",
              "    pm.response.to.have.status(400);",
              "});"
            ]
          }
        }
      ],
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/accounts/{{accountId}}/deposit",
          "host": [
            "{{baseUrl}}"
          ],
          "path": [
            "api",
            "accounts",
            "{{accountId}}",
            "deposit"
          ]
        },
        "body": {
          "mode": "raw",
          "raw": "{\n  \"amount\": -5\n}",
          "options": {
            "raw": {
              "language": "json"
            }
          }
        }
      }
    },
    {
      "name": "Delete the account opened above",
      "event": [
        {
          "listen": "test",
          "script": {
            "type": "text/javascript",
            "exec": [
              "pm.test(\"Status code is 204\", function () {",
              "    pm.response.to.have.status(204);",
              "});"
            ]
          }
        }
      ],
      "request": {
        "method": "DELETE",
        "header": [],
        "url": {
          "raw": "{{baseUrl}}/api/accounts/{{newAccountId}}",
          "host": [
            "{{baseUrl}}"
          ],
          "path": [
            "api",
            "accounts",
            "{{newAccountId}}"
          ]
        }
      }
    }
  ],
  "variable": [
    {
      "key": "baseUrl",
      "value": "http://localhost:5000"
    },
    {
      "key": "accountId",
      "value": "1"
    }
  ]
}
""";

    public const string HttpFile = """
# Bank API requests for the VS Code REST Client extension or Visual Studio's .http editor.
# Change @baseUrl to wherever the API is running. This API has no login, so there are no secrets here.
# These files are for an API you run as its own program; this lesson has no such program, so it replays the same requests against the in-process copy.
# Requests are separated by a line that starts with ###. Click 'Send Request' above a request to run it.
# The last request deletes the account opened by 'createAccount', so the file can be run again against the same API.

@baseUrl = http://localhost:5000
@accountId = 1

### List all accounts (expect 200)
GET {{baseUrl}}/api/accounts

### Get one account (expect 200)
GET {{baseUrl}}/api/accounts/{{accountId}}

### Get an account that does not exist (expect 404)
GET {{baseUrl}}/api/accounts/999

### Open a new account (expect 201)
# @name createAccount
POST {{baseUrl}}/api/accounts
Content-Type: application/json

{
  "owner": "Chen Li",
  "accountType": "Savings"
}

### Open an account with an invalid type (expect 400)
POST {{baseUrl}}/api/accounts
Content-Type: application/json

{
  "owner": "Chen Li",
  "accountType": "Gold"
}

### Deposit money (expect 200)
POST {{baseUrl}}/api/accounts/{{accountId}}/deposit
Content-Type: application/json

{
  "amount": 50.25
}

### Deposit a negative amount (expect 400)
POST {{baseUrl}}/api/accounts/{{accountId}}/deposit
Content-Type: application/json

{
  "amount": -5
}

### Delete the account opened above (expect 204)
DELETE {{baseUrl}}/api/accounts/{{createAccount.response.body.$.id}}
""";
}
