# Synapse Server Controller Web API

The Synapse Controller Web API manages enforcing the RBAC, starting, cancelling, and recording/fetching Plan status.

## Executing Plans

The Synapse Controller URI is: http://{host:port}/synapse/execute.  For detailed method invocation information, see the Swagger URI at: http://{host:port}/swagger/

|Verb|URI|Description
|-|-|-
|get|/synapse/execute/hello|Returns "Hello, World!"  Does not invoke RBAC or DAL, but does require authentication.
|get|/synapse/execute/hello/whoami|Returns a string of the authenticated user context.  Does not invoke RBAC or DAL.
|get|/synapse/execute/{planUniqueName}/item|Returns a Plan by PlanUniqueName.
|get|/synapse/execute/{planUniqueName}/help|Returns Plan Dynamic Parameter information by PlanUniqueName.
|get|/synapse/execute|Returns a list of Plans.
|get|/synapse/execute/{planUniqueName}|Returns a list of Plan Instance Ids.
|get|/synapse/execute/{planUniqueName}/start|Execute a Plan using the URI querystring for dynamic parameters. Returns planInstanceId.
|post|/synapse/execute/{planUniqueName}/start|Execute a Plan with an http post, where dynamic parameters are specified in the http body. Returns planInstanceId.
|get|/synapse/execute/{planUniqueName}/start/sync|Execute a Plan using the URI querystring for dynamic parameters and polls for completion at the server.  Includes an embedded call to `/part/` and, by default, returns `Actions[0]:Result:ExitData`.  See [_Using the Part Interface_](#using-the-part-interface) below for more information.
|post|/synapse/execute/{planUniqueName}/start/sync|Execute a Plan with an http post, where dynamic parameters  are specified in the http body and polls for completion at the server.  Includes an embedded call to `/part/` and, by default, returns `Actions[0]:Result:ExitData`.  See [_Using the Part Interface_](#using-the-part-interface) below for more information.
|delete|/synapse/execute/{planUniqueName}/{planInstanceId}|Cancel a Plan by planInstanceId.
|get|/synapse/execute/{planUniqueName}/{planInstanceId}|Get the status of a Plan by planInstanceId.
|post|/synapse/execute/{planUniqueName}/{planInstanceId}|Update the status of the entire Plan, by planInstanceId.
|post|/synapse/execute/{planUniqueName}/{planInstanceId}/action|Update the status of an individual Action within a Plan, by planInstanceId.
|get|/synapse/execute/{planUniqueName}/{planInstanceId}/part|Select an individual element from within a Plan, specifying the desired return data serialization format.  See [Using the Part Interface] below for details.
|post|/synapse/execute/{planUniqueName}/{planInstanceId}/part|Select one or more individual elements from within a Plan, specifying the desired return data serialization format.  See [Part] below for details.

# Synchronous and Asynchronous Execution Models

Synapse supports two execution models: sync and async.  These should not be confused with async/await threading patterns as implemented by client consumers - rather, the models refer to the behavior of the Controller when initiating a Plan.

## Synchronous Execution

When executing under the sync interface, the Controller will monitor the Plan for completion and return a subset of the ResultPlan when complete.  "Complete" is defined as a Plan Status value of `Complete/Success` (integer 128) or greater ([_see below for StatusType detail_](#statustype-values)).  Sync execution is most appropriate for short-running Plans where maintaining an open HTTP connection is acceptable.

### Sync Execution Options

|Parameter|Type|Required|Default|Description
|-|-|-|-|-
|`planUniqueName`|string|yes|none|The unique name of the Plan to execute.
|`dryRun`|boolean|no|`false`|Indicates whether to execute the Plan in a "test" mode.
|`requestNumber`|string|no|none|Optional self-provided tracking number for this current Plan execution.
|`path`|string|**no**|`Actions[0]:Result:ExitData`|The subsection of the ResultPlan to return when the Plan completes.  See [Path Syntax](#path-syntax) for details.
|`serializationType`|[SerializationType](#serializationtype-values)|no|`SerializationType.Json`|Takes the output from `path` above and validates it can be parsed according to the SerializationType and returns it as such.
|`setContentType`|boolean|no|`true`|Optionally sets the HTTP Response header for `Content-Type` to match the SerializationType.
|`pollingIntervalSeconds`|int|no|1|Specifies time interval, in seconds, for how often the Controller polls for Plan completion.
|`timeoutSeconds`|int|no|120|Specifies the time interval, in seconds, for how long the Controller will continues polling for Plan completion.  If the timeout elapses the Controller will throw a timeout exception but that _does not_ affect Plan execution - you may continue to poll for status manually following a timeout.
|`nodeRootUrl`|string|no|none|Optional value to override the default Node for Plan execution.  Specify as `http://{host:port}/synapse/node`.

### Example GET/POST

Notes:

 - Parameters specified below for clarity, see above for required/default values.
 - GET/POST URLs are the same, but Plan Dynamic Parameters are managed differently per method.  See [Dynamic Parameters](#dynamic-parameters) below for details.

`http://localhost:20000/synapse/execute/samplePs1/start/sync?dryRun=false&requestNumber=1234&path=Actions%5B0%5D%3AResult%3AExitData&serializationType=Json&setContentType=true&pollingIntervalSeconds=2&timeoutSeconds=20`


#### StatusType Values
StatusType values are part of the Synapse Core project.   The values below are current as of the last update to this documentation,
but for the most recent values, please check the [SynapseProject GitHub page](https://github.com/SynapseProject/synapse.core.net/blob/master/Synapse.Core/Classes/Enums/StatusType.cs).
```java
namespace Synapse.Core
{
    public enum StatusType
    {
        None = 0,
        New = 1,
        Initializing = 2,
        Running = 4,
        Waiting = 8,
        Cancelling = 16,
        Complete = 128,
        Success = 128,
        CompletedWithErrors = 256,
        SuccessWithErrors = 256,
        Failed = 512,
        Cancelled = 1024,
        Tombstoned = 2048,
        Any = 16383
    }
}
```

#### Path Syntax
Selecting Plan elements requires a rooted path, expressed as shown below.  If an element is part of a list, use a index specifier.

- element:element:element:etc.
- element[index]:element:element[index]:element:etc.

Examples:

- `Result:BranchStatus` #returns Plan.Result.Status
- `Actions[0]:Result:ExitData`  #returns Plan.Actions[0]:Result:ExitData


#### SerializationType Values
StatusType values are part of the Synapse Core project.   The values below are current as of the last update to this documentation,
but for the most recent values, please check the [SynapseProject GitHub page](https://github.com/SynapseProject/synapse.core.net/blob/master/Synapse.Core/Classes/Enums/SerializationType.cs).
```java
namespace Synapse.Core
{
    public enum SerializationType
    {
        Yaml = 0,
        Xml = 1,
        Json = 2,
        Html = 3,
        Unspecified = 4
    }
}
```

YAML/JSON/XML will attempt to parse the element data into the specified format before returning it.  "Unspecified" will return the data, as-is.


## Asynchronous Execution

When executing under the async interface, the Controller will immediately return a plan instance Id back to the caller and close the HTTP connection.  To know when the Plan has completed, create a manual poller ([example](#example-manual-poller)) and use the API endpoints for status as shown below.  Asynchronous execution is most appropriate for long-running Plans where maintaining an open HTTP connection is inappropriate.  When using async execution, you will need to manually poll for Plan completion via the planInstanceId, which may be accomplished through two interface options, as described below.

### Async Execution Options

|Parameter|Type|Required|Default|Description
|-|-|-|-|-
|`planUniqueName`|string|yes|none|The unique name of the Plan to execute.
|`dryRun`|boolean|no|`false`|Indicates whether to execute the Plan in a "test" mode.
|`requestNumber`|string|no|none|Optional self-provided tracking number for this current Plan execution.
|`nodeRootUrl`|string|no|none|Optional value to override the default Node for Plan execution.  Specify as `http://{host:port}/synapse/node`.

### Example GET/POST

Notes:

 - Parameters specified below for clarity, see above for required/default values.
 - GET/POST URLs are the same, but Plan Dynamic Parameters are managed differently per method.  See [Dynamic Parameters](#dynamic-parameters) below for details.

`http://localhost:20000/synapse/execute/samplePs1/start?dryRun=false&requestNumber=1234`


### Example Manual Poller
```java
public static StatusType GetStatus(string planName, long id, int pollingIntervalSeconds = 1, int timeoutSeconds = 120)
{
    //ensure valid values
    pollingIntervalSeconds = pollingIntervalSeconds < 1 ? 1 : pollingIntervalSeconds;
    timeoutSeconds = timeoutSeconds < 1 ? 120 : timeoutSeconds;

    int c = 0;
    StatusType status = StatusType.New;
    while( c < timeoutSeconds )
    {
        Thread.Sleep( pollingIntervalSeconds * 1000 );
        //pseudo-code: status = fetch from whole ResultPlan or part interface
        c = status < StatusType.Success ? c + 1 : int.MaxValue;
    }

    return status;
}
```

### Getting Plan Status with PlanInstanceId

When manually fetching Plan status, you may choose to retrieve the entire `ResultPlan` or a specified subsection via the `\part` interface.

#### Getting the Entire ResultPlan

One way to fetch Status is via getting the entire ResultPlan.  When polling, the ResultPlan will not contain status for every Action until the Plan Status is >= Complete.  To retrieve the ResultPlan, execute an HTTP GET to:

- `/synapse/execute/{planUniqueName}/{planInstanceId}`

Then, parse the Plan and get the Plan.Result.Status.


#### Getting Subsections of the ResultPlan with the `/part` Interface

The Synapse Controller /part interface is designed to allow selection of one or more individual Plan elements, serialized into a specific format.  This is useful when seeking efficiency of data interrogation or when serializing data to a specific format.  This capability is embedded in Synchronous Plan execution and is the method by which data is returned.

|Parameter|Type|Required|Default|Description
|-|-|-|-|-
|`path`|string|**yes**|`Actions[0]:Result:ExitData`|The subsection of the ResultPlan to return when the Plan completes.  See [Path Syntax](#path-syntax) for details.
|`serializationType`|[SerializationType](#serializationtype-values)|no|`SerializationType.Json`|Takes the output from `path` above and validates it can be parsed according to the SerializationType and returns it as such.
|`setContentType`|boolean|no|`true`|Optionally sets the HTTP Response header for `Content-Type` to match the SerializationType.

### Example GET

Notes:

 - Parameters specified below for clarity, see above for required/default values.
 - Unlike Synchronous Plan execution, Path is **required** in the /part interface.

`http://localhost:20000/synapse/execute/samplePs1/12341234/part?elementPath=Actions%5B0%5D%3AResult%3AExitData&serializationType=Json&setContentType=true`

### Example POST

- URL: `http://localhost:20000/synapse/execute/samplePs1/12341234/part`
- Body:

```
{
  "Type": "Json",
  "ElementPaths": [
    "Actions[0]:Result:ExitData",
    "Result.Status"
  ]
}
```

## Dynamic Parameters

Synapse Controller accepts dynamic Plan parameters in both GET and POST operations, as follows below.

### Dynamic Parameters with GET

The general URI signature for starting a Plan via GET is:

- `http://host:port/synapse/execute/{planUniqueName}/start/?dryRun={true|false}&requestNumber={requestNumber}`
- `http://host:port/synapse/execute/{planUniqueName}/start/sync/?dryRun={true|false}&requestNumber={requestNumber}`

where `dryRun` and `requestNumber` are reserved by Synapse as built-in parameters.  Every other key/value pair on the URL is passed-through to the Plan as a custom/dynamic parameter.

#### GET Example:

- `http://host:port/synapse/execute/{planUniqueName}/start/?dryRun=true&requestNumber=0123&key0=value0&key1=value1&key2=value2`
- `key0`, `key1`, and `key2` will all be treated as dynamic parameters and passed through to the Plan.

### Dynamic Parameters with POST

As with a GET, the URI for a POST remains:

- `http://host:port/synapse/execute/{planUniqueName}/start/?dryRun={true|false}&requestNumber={requestNumber}`
- `http://host:port/synapse/execute/{planUniqueName}/start/sync/?dryRun={true|false}&requestNumber={requestNumber}`

but sending dynamic parameters is accomplished in the http request via the `DynamicParameters` key/value pair wrapper structure.

#### POST Example

- Content-Type = application/json
- Request Body (raw):

```json
{
  "DynamicParameters": {
    "key0": "value0",
    "key1": "value1",
    "key2": "value2"
  }
}
```

# Synapse Server Node Web API

The Synapse Node Web API manages Plan runtime execution.  For information about configuration, see [Controller/Node Setup](setup "Controller/Node Setup").

#### About Synapse Node Security

Generally, Synapse Nodes can run as Anonymous, but they should always implement the SignPlan/VerifyPlanSignature construct to guarantee Plans are invoked by authorized users from authorized Controllers.  You may apply additional security setup, such as higher levels of authentication or SSL/TLS, if desired. 

## Executing Plans

The Synapse Node URI is: http://{host:port}/synapse/node.  For detailed method invocation information, see the Swagger URI at: http://{host:port}/swagger/

|Verb|URI|Description
|-|-|-
|get|/synapse/node/hello|Returns "Hello, World!"  Does not invoke a Handler, but does require authentication.
|get|/synapse/node/hello/whoami||Returns a string of the authenticated user context.  Does not invoke a Handler.
|get|/synapse/node/hello/about|Returns the server configuration (`synapse.server.config.yaml`) and an inventory of files for this server instance.
|delete|/synapse/node/{planInstanceId}|Cancels a Plan by planInstanceId.  _Note:_ Plan Handlers must detect a request to Cancel and exit gracefully.  Synapse Node does not forcefully abort Plan execution at this time.
|post|/synapse/node/{planInstanceId}|Starts a Plan using the URI querystring for dynamic parameters.
|post|/synapse/node/{planInstanceId}/p|Starts a Plan with an http post, where dynamic parameters are specified in the http body.
|get|/synapse/node/drainstop|Pauses the Node from accepting new Plans for execution, allows the existing queue of Plans to complete.  Optionally Stops the Service when fully drainstopped.
|get|/synapse/node/drainstop/cancel|Returns the Node to normal execution.  This command is only effective before the queue is fully drained when drainstop->shutdown = true.
|get|/synapse/node/drainstop/iscomplete|Returns the queue drainstop status.
|get|/synapse/node/queue/count|Returns the number of items in the execution queue.
|get|/synapse/node/queue|Returns a list of Plans in the execution queue.
|get|/synapse/node/update|Invokes AutoUpdate, which will stop the server, refresh the binaries, and then optionally restart the server.
|get|/synapse/node/update/logs|Fetches a list of AutoUpdate logs.
|get|/synapse/node/update/logs/{name}|Fetches a specific AutoUpdate log.