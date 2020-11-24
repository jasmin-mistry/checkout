# Bank.Client

Bank.Client is a dotnet/c# NetStandard 2.0 class library SDK to enable interaction with the Bank APIs.

# How to use Bank.Client

Add BankClient in ServiceCollection of your project

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // other dependencies

    // Add bank client to your available services
    services.AddBankClient(Configuration);
}
```

Add the config for the API to your appsettings.json. You can connect with Azure Active Directory ClientID and Client Secret or using Managed Identity.

Either using client credentials:

```json
{
  "bank-api-options": {
    "apiHost": "bank.com/api/v1",
    "authApiHost": "bank.com",
    "ApiClientSecret": "client-secret",
    "ApiClientId": "client-id",
    "ApiSubscriptionKey": "someApiKey"
  }
}
```

Or using managed identity:

```json
{
  "bank-api-options": {
    "apiHost": "bank.com/api/v1",
    "authApiHost": "bank.com",
    "ApiSubscriptionKey": "someApiKey",
    "IsManagedIdentity": "true"
  }
}
```

It can be used using DI. BankClient has following clients available to get the corresponding data:

- **IPaymentClient:** Process the payment

```csharp
private IBankClient bankClient

public MyClass(IBankClient client)
{
    this.bankClient = client;
}

 public async Task ProcessPayment()
 {
    var response = await bankClient.Payment.Process(payment);

    if(response.StatusCode == HttpStatusCode.OK)
    {
        var paymentResponse =  response.Content;
        // do something with the payment response
    }
 }
```
