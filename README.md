
# TRIPRO.BotFramework.Auth.AspNetCore for C# .Net Core 2.0 (Azure AD B2C)
TRIPRO.BotFramework.Auth.AspNetCore is a package for handling authentication in a bot built using the Microsoft Bot Framework and BotBuilder libraries. It contains a core library that handles authentication, a set of providers that are dependency injected into the core library, and sample that leverage the providers. More specific details are listed below:

## [TRIPRO.BotFramework.Auth.AspNetCore](/TRIPRO.BotFramework.Auth.AspNetCore)
BotAuth is the core library that contains the following important files:
- [**Dialogs/AuthDialog.cs**](/TRIPRO.BotFramework.Auth.AspNetCore/Dialogs/AuthDialog.cs): the dialog class for initiating the OAuth flow
- [**Controllers/CallbackController.cs**](/TRIPRO.BotFramework.Auth.AspNetCore/Controllers/CallbackController.cs): the callback controller for getting access tokens from an authorization code
- [**Providers/IAuthProvider.cs**](/TRIPRO.BotFramework.Auth.AspNetCore/Providers/IAuthProvider.cs): the interface that all providers need to implement
- [**Models/AuthenticationOptions.cs**](/TRIPRO.BotFramework.Auth.AspNetCore/Models/AuthenticationOptions.cs): class used to initialize app details (ex: app id, app secret, scopes, redirect, etc) and passed into the AuthDialog
- [**Models/AuthResult.cs**](/TRIPRO.BotFramework.Auth.AspNetCore/Models/AuthResult.cs): the result passed back from the AuthDialog

## Using TRIPRO.BotFramework.Auth.AspNetCore
To use TRIPRO.BotFramework.Auth.AspNetCore, you should install the NuGet package of the provider(s) you want to use (see NuGet section for more information on packages). Currently, TRIPRO.BotFramework.Auth.AspNetCore has provider for Azure B2C applications.
BotAuth is invoked by initializing an instance of AuthenticationOptions with your app details and passing that into the AuthDialog with a provider instance (implementing IAuthProvider). In the sample below, the AADb2c provider is used. The AuthDialog returns an access token when the dialog resumes.

```CSharp
public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
{
	// authOptions is defined in appsettings.json
    await context.Forward(new AuthDialog(new AADb2cAuthProvider(), authOptions), async (IDialogContext authContext, IAwaitable<IAuthResult> authResult) =>
    {
        AuthResult result = (AuthResult)await authResult;

        // Use TriProAuthResult
        await authContext.PostAsync($"I'm a simple bot that doesn't do much, but I know your name is {result.UserName} and you are logged in using {result.IdentityProvider}");

        authContext.Wait(MessageReceivedAsync);

    }, context.MakeMessage(), CancellationToken.None);
}
```

Each of the providers have slight differences (largely in the use of AuthenticationOptions), so refer to the samples for provider-specific logic.

## Magic Numbers
BotAuth implements a magic number to provide additional security in group chats. This is an important addition to securing user-specific tokens in the correct user data and is defaulted to ON. In 1:1 chatbots, the magic number can be turned off by setting the **AuthenticationOptions** property for **UseMagicNumber** to false. All of the samples use the magic number, but can easily be disabled with this UseMagicNumber property.

## Samples
The repo contains 1 working bot sample. It is published without app details so you have to configure it in appsettings.json and then you can debbug it locally. There is a sample which uses its own provider and AuthResult that inherits AuthResult and implements custom policy properties.

## Creating custom providers
To build a custom provider, you must install the BotAuth.AspNetCore core package into your project and create a class that implements the IAuthProvider interface. This interface has 5 functions and a property you need to implement so the provider can be dependency injected into the BotAuth core library at runtime. Refer to the existing providers if you want more information on how to implement this interface.

We altered only AADb2cProvider, other providers from [original project](https://github.com/MicrosoftDX/botauth) are missing in this repository, feel free to contribute and add them.

## Creating custom AuthResult
To build a custom AuthResult, you must install the BotAuth.AspNetCore core package into your project and create a class that inherits the AuthResult class which implements IAuthResult interface. This interface defines only properties which have to be defined in your custom AuthResult class. Refer to the existing TriProAuthResult if you want more information on how to implement custom AuthResult.

## NuGet
Provider and the core library are published to NuGet. If you are using a provider, the core library comes in automatically as a dependency. You should only leverage the core library directly if you are building a provider.

## Reference
This is .Net Core 2.0 version of the following [Microsoft DX BotAuth CSharp repository](https://github.com/MicrosoftDX/botauth/tree/master/CSharp)