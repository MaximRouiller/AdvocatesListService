# AdvocatesListService

Small API that allows to create a list of Microsoft Cloud Advocates in API form.

This API retrieves publically available information from https://github.com/MicrosoftDocs/cloud-developer-advocates and present them into a publically available service in JSON format.

## Service URL

https://advocateslistservice.azurewebsites.net/api/Advocates

## Sample data

```json
[
  {
  "gitHubUsername": "user1",
  "microsoftAlias": "alias1",
  "team": "team1"
  },
  {
  "gitHubUsername": "user2",
  "microsoftAlias": "alias2",
  "team": "team2"
  }
]
```
