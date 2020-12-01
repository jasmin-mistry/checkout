# Checkout.com | Payment Gateway

## Payment Gateway API

The Payment Gateway API provides merchants with a way to process a payment. The merchant will be able to submit a request to the payment gateway. A payment request will include fields such as the card number, expiry month, expiry date, amount, currency, and cvv.

## Bank Api - Api Stub

Bank API able to process the payment on behalf of the acquiring bank. The response will return a unique identifier and a status that indicates whether the payment was successful or not. If not the response will also include a reason why the request failed.

### Bank API Client

Bank Client also publishes Bank.Client.BankClient to access the Acquiring Bank APIs. Check the readme [here](/src/BankApi/Bank.Client/README.md).

## Assumptions

- The failure response received from the Bank API will return 400 (BadRequest) status code with unique id, status and reason (why the process failed). 
- For test, only In-Memory data store has been used. For production purpose that can be replace by using SQL Server. This can be achieved by updating the DI for the `AppDbContext` class. An example is already provided in the [Startup.cs](/src/PaymentGateway/Startup.cs)
- The payment model was kept to minimum to avoid complexity of the test but for production system the data model needs to be designed considering various aspect like security, GDPR, encryption etc.
- Masking the Card Number when retrieving details of a previously made payment using `GET /payments/{paymentId}` API was only done for the middle 8 digits using Regex pattern. But this can be changed by updating the Regex pattern in MaskCardNumber method in [StringExtensions.cs](/src/SharedKernel/StringExtensions.cs).
- Cvv field was not returned as part of `GET /payments/{paymentId}` API, because these information is only needed for payment processing.
- HTTP Handlers for authentication (using mock) and exception handling is added to Bank API.
- Process payment and retrive previous payment is implemented using Mediatr pattern,
- Validating the payload for process payment `POST /payments/` API is implemented using the FluentValidation and MediatR.IPipelineBehavior. This includes a Regex pattern for card number but not the validating all type of cards or using an credit/debit card validation algorithm (which I believe should be part of Bank API functionality).
- Healthcheck for the Payment Gateway API is added to check the dependency (in this case Bank API) is working. The assumption is when the Bank API return 502 (BadGateway) status code, the health check should report the API is unhealthy.
- I have also assumed that the Bank API will always return 400 (BadReqeust) status when Card Number is "9999-9999-9999-9999".

## Improvements

- For data persistence, SQL Server database can replace the In-Memory store.
- A middleware can added in the PaymentGateway API middleware pipeline to attach "Correlation ID" or "Trace ID" (a unique identifier value) to requests and messages that allow reference to a particular transaction or event chain.
- A middleware can be added to log any outbound events/calls made from Payment Gateway API.
- Authentication for Payment Gateway API can be added.
- Separate the Process Payment and Retrieve Payment into seperate APIs to allow scalability as I think processing payment is more important compared to retrieve payment functionality.
- An API client for Payment Gateway API would be nice (similar to [BankClient](/src/BankApi/Bank.Client/BankClient.cs) using Refit), if the client needs to be used in the .Net project. But for any other client NSwagStudio can be used see [Payment Gateway API Client](#Payment-Gateway-API-Client) section. 

## Dev - Dependencies

### Tools
- Visual Studio Code or 2019
- Docker
- Powershell

### Libraries
- [EF Core](https://docs.microsoft.com/en-us/ef/) : database mapper for .NET
- [MediatR](https://github.com/jbogard/MediatR/wiki) : to facilitate two architecture patterns: CQRS and the Mediator pattern. 
- [FluentValidation](https://fluentvalidation.net/) : small validation library for .NET that uses a fluent interface and lambda expressions for building validation rules.
- [Swashbuckle.Aspnetcore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) : for [Swagger](https://swagger.io/) tooling for API's built with ASP.NET Core
- [Refit](https://github.com/reactiveui/refit) : The automatic type-safe REST library for .NET Core, Xamarin and .NET
- [LazyCache](https://github.com/alastairtree/LazyCache) : simple in-memory caching service.

## Payment Gateway API Client

Code for Payment Gateway API Client can be auto-generated using [NSwagStudio](https://github.com/RicoSuter/NSwag/wiki/NSwagStudio). NSwag is the OpenAPI/Swagger API toolchain for .NET, .NET Core, Web API, ASP.NET Core, TypeScript (jQuery, AngularJS, Angular 2+, Aurelia, KnockoutJS and more) and other platforms.

![NSwagStudio](images/NSwagStudioUsage.gif)


## Build using Powershell

Run the below command to build the solution locally;

Execute [build.ps1](/src/build.ps1) to compile application and package release artifacts. Outputs to `/dist/` folder.

The build script has configurable options which helps split build execution during the CI pipeline process. Below are some examples;

- Full Build: `PS> .\build.ps1`
- Debug Build: `PS> .\build.ps1 -Config "Debug"`
- Fast Build: `PS> .\build.ps1 -SkipTests`
- UI Client Build: `PS> .\build.ps1 -SkipBackend`

The build script also outputs the code coverage result. The coverage report files can be found under `dist\Coverage` folder. After the build process is executed you can open the coverage report using `dist\Coverage\index.html`

![running full build](images/build.gif)

## Build using Docker

The Payment Gateway API and Bank API Stub is an ASP.Net core application intended to run as a docker container.

To Build, (re)create container image for service(s), run the below command:

``` shell
docker-compose build
```

To start, and attach to container for service(s), run: 

``` shell
docker-compose up
```

## Build using `dotnet` CLI

``` shell
dotnet build Checkout.sln
```

``` shell
dotnet test Checkout.sln
```

### To run the Payment Gateway API

Before running the below `dotnet run` command, please update the `appsettings.json` file as shown below;

- in `src\PaymentGateway\appsettings.json`, update the `bank-api-options.ApiHost`  to `"localhost:5001"`
- in `src\BankApi\ApiStub\appsettings.json`, update the `WireMockServerSettings.Urls` to `["http://localhost:5001"]`

``` shell
cd "src\PaymentGateway\"
dotnet run PaymentGateway.csproj
```

To run the Bank API - ApiStub 
``` shell
cd "src\BankApi\ApiStub\"
dotnet run ApiStub.csproj
```

## How to build and run multiple projects

Below are the video links to the screen recording that shows how to build, run and test the Payment Gateway API integrated with Bank API (ApiStub)

[Using dockercompose via command line](https://youtu.be/nJziB89L1ZQ)

[Run Multiple Startup Projects On VS2019](https://youtu.be/mHGy1guzI0M)

[Run Using DockerCompose On VS2019 ](https://youtu.be/Qp881QRFvXc)

## Sample payload for Payments API

- sample payload for success `POST /api/payments/` API

```json
{
  "cardNumber": "1234-1234-1234-1234",
  "expiryMonth": "Mar",
  "expiryYear": "2021",
  "amount": 12340,
  "currency": "GBP",
  "cvv": "123"
}
```

- sample payload for failed `POST /api/payments/` API

```json
{
  "cardNumber": "9999-9999-9999-9999",
  "expiryMonth": "Mar",
  "expiryYear": "2021",
  "amount": 12340,
  "currency": "GBP",
  "cvv": "123"
}
```

