# fibonacci (event)

```
GET /fibonacci
  ?count={count}
--- 200 OK (server-sent events)
{
  "value": (integer)
}
```

| request | type | description |
| --- | --- | --- |
| count | int32 |  |

| response | type | description |
| --- | --- | --- |
| value | int32 |  |

<!-- DO NOT EDIT: generated by fsdgenmd -->
