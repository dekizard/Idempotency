# Idempotency implementation in .NET 6

Idepotency means that making multiple identical requests has the same effect as making a single request.  
For example, POST request is not idempotent.  
Do API retry without idempotency may create duplicated resources.  

Including an idempotency key makes POST requests idempotent, which prompts the API to do the record keeping required to prevent duplicate operations.   
If a request to create a transaction doesn’t respond because of a network connection error, a client can retry the request with the same idempotency key to guarantee that no more than one transaction is created.  


Idempotency key is sent in the Idempotency-Key header, and you should use them for all POST requests, for generating idempotency keys UUID V4 is used:  
Previously executed response that’s being replayed from the server has additional resonse header: Cahched-Response:true.  

In this implementation there are two kind of validations:  
1)Check if there are concurrent requests with the same idempotency key.
If so, 409 Conflict is returned.
Here, the same requests implies the same request path and body.
2)Check if already exists cached request with the same idempotency key. 
Return 400 Bad request if it exists.  

For production Redis distributed cache service could be used.
For testing purpose memory caching can be sufficient (item are stored by the app instance on the server where the app is running).  

Idempotency keys expires out of the system after defined number of hours.
