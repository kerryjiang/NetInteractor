# NetInteractor
[![Build Status](https://travis-ci.org/kerryjiang/NetInteractor.svg?branch=master)](https://travis-ci.org/kerryjiang/NetInteractor)
[![NuGet Version](https://img.shields.io/nuget/v/NetInteractor.Core.svg?style=flat)](https://www.nuget.org/packages/NetInteractor.Core/)

Web operation automation library in C# (.NET Core)

It can be used for the purposes below:
* Web white-box test automation;
* Web operation automation;
* ...

Features:
* Xml as script;
* Accept input parameters;
* Extract parameters from the responses of middle steps (output parameters);
* Basic work flow controlling (if);


## The Automation Script

![Shop](assets/config.png)


## Execute the script
```csharp
var config = ConfigFactory.DeserializeXml<InteractConfig>("Scripts/Shop.config");

var executor = new InterationExecutor();

var inputs = new NameValueCollection();

inputs["CreditCardNumber"] = "0123456789ABCD";
inputs["CreditCardExpMonth"] = "04";
inputs["CreditCardExpYear"] = "2021";
// more input parameters
// ...

var result = await executor.ExecuteAsync(config, inputs);

```
