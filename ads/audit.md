# Auditing

So, the app has money and real stakes in it. We can't just let people change states willy nilly, and a simple CreatedAt/By ModifiedAt/By scheme won't work. We need auditing. But opt-in for specific white-listed entities only, since audit logging is slow and unnecessary for, say, login.

Note that I do know `Audit.NET` exists, but we may warrant a custom solution since auditing rarely changes, is quite trivial to implement and is not so hard that it outweights the risk of adding another dependencies.

## Centralized table

To keep things simple (and cleaner code too) there will be a single entity/DB schema to store audit logs of the entire app. This will have:

- uuid Id (PK)
- ActionEnum(Insert, Delete, Update) Action
- string TableName
- uuid TargetId
- jsonb 
- jsonb
- timestamptz ChangedAt