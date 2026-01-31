# NetInteractor

[![build](https://github.com/kerryjiang/NetInteractor/actions/workflows/build.yml/badge.svg)](https://github.com/kerryjiang/NetInteractor/actions/workflows/build.yml)
[![NuGet Version](https://img.shields.io/nuget/v/NetInteractor.svg?style=flat)](https://www.nuget.org/packages/NetInteractor/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NetInteractor.svg)](https://www.nuget.org/packages/NetInteractor/)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

A powerful web operation automation library for .NET that enables you to define and execute complex web interactions using simple XML scripts.

## 🎯 Use Cases

- **Web Testing**: Automate white-box and integration testing for web applications
- **Web Scraping**: Extract data from websites using XPath expressions
- **Form Automation**: Automate form submissions and multi-step workflows
- **E-commerce Automation**: Automate checkout processes, cart management, and more

## ✨ Features

- **XML-based Scripts**: Define web interactions in easy-to-read XML configuration files
- **Input Parameters**: Pass dynamic values into your automation scripts using `$(ParameterName)` syntax
- **Output Extraction**: Extract values from HTTP responses using XPath or Regex
- **Flow Control**: Conditional execution with `if` statements
- **Reusable Targets**: Define named targets and call them from other targets
- **Modern HTTP Client**: Uses `HttpClient` for better performance and reliability
- **Cross-Platform**: Supports .NET Standard 2.0, .NET 8.0, and .NET 9.0
- **Browser Automation**: Optional Playwright integration for JavaScript-rendered pages

## 📦 Installation

Install via NuGet Package Manager:

```bash
dotnet add package NetInteractor
```

For browser automation with JavaScript rendering, also install the Playwright package:

```bash
dotnet add package NetInteractor.Playwright
```

Or via the Package Manager Console:

```powershell
Install-Package NetInteractor
```

## 🚀 Quick Start

### 1. Create an XML Script

Create a configuration file (e.g., `Shop.config`) that defines your web interaction workflow:

```xml
<InteractConfig defaultTarget="BuyItem">
    <target name="BuyItem">
        <!-- Navigate to product page -->
        <get url="http://www.example-shop.com/product/12345" />
        
        <!-- Add item to cart -->
        <post clientID="cart-form">
            <formValue name="size" text="Medium" />
            <formValue name="quantity" value="1" />
        </post>
        
        <!-- Verify cart contents -->
        <get url="http://www.example-shop.com/cart">
            <output name="totalPrice" 
                    xpath="//span[@class='cart-total']" 
                    attr="text()" 
                    expectedValue="$28.00" />
        </get>
        
        <!-- Checkout with payment details -->
        <post clientID="checkout_form">
            <formValue name="billing_name" value="$(FullName)" />
            <formValue name="email" value="$(Email)" />
            <formValue name="credit_card" value="$(CreditCardNumber)" />
            <formValue name="exp_month" value="$(CreditCardExpMonth)" />
            <formValue name="exp_year" value="$(CreditCardExpYear)" />
        </post>
    </target>
</InteractConfig>
```

### 2. Execute the Script

```csharp
using NetInteractor;
using NetInteractor.Config;
using NetInteractor.WebAccessors;
using System.Collections.Specialized;

// Load the configuration
var config = ConfigFactory.DeserializeXml<InteractConfig>("Scripts/Shop.config");

// Create the executor with an HTTP client accessor
var webAccessor = new HttpClientWebAccessor(new HttpClient());
var executor = new InterationExecutor(webAccessor);

// Prepare input parameters
var inputs = new NameValueCollection
{
    ["FullName"] = "John Doe",
    ["Email"] = "john@example.com",
    ["CreditCardNumber"] = "4111111111111111",
    ["CreditCardExpMonth"] = "12",
    ["CreditCardExpYear"] = "2027"
};

// Execute the automation
var result = await executor.ExecuteAsync(config, inputs);

// Check results
if (result.Ok)
{
    Console.WriteLine("Automation completed successfully!");
    
    // Access extracted output values
    foreach (var key in result.Outputs.AllKeys)
    {
        Console.WriteLine($"{key}: {result.Outputs[key]}");
    }
}
else
{
    Console.WriteLine($"Automation failed: {result.Message}");
}
```

### 3. Using Playwright for JavaScript-Rendered Pages

For websites that require JavaScript rendering (e.g., SPAs, dynamic content), use the Playwright web accessor:

```csharp
using NetInteractor;
using NetInteractor.Config;
using NetInteractor.WebAccessors;

// Load the configuration
var config = ConfigFactory.DeserializeXml<InteractConfig>("Scripts/Shop.config");

// Create the executor with a Playwright accessor for JavaScript support
await using var webAccessor = new PlaywrightWebAccessor();
var executor = new InterationExecutor(webAccessor);

// Execute the automation (same as before)
var result = await executor.ExecuteAsync(config, inputs);
```

> **Note**: Playwright requires browser binaries. Run `playwright install chromium` to install the required browser.

## 📖 Script Reference

### Actions

| Action | Description |
|--------|-------------|
| `<get>` | Performs an HTTP GET request |
| `<post>` | Submits a form via HTTP POST |
| `<if>` | Conditional execution based on parameter values |
| `<call>` | Calls another target by name |

### GET Action

Navigates to a URL and optionally extracts output values:

```xml
<get url="http://example.com/page">
    <output name="title" xpath="//h1" attr="text()" />
    <output name="price" xpath="//span[@class='price']" attr="text()" expectedValue="$10.00" />
</get>
```

### POST Action

Submits a form with the specified values:

```xml
<!-- By form client ID -->
<post clientID="login-form">
    <formValue name="username" value="$(Username)" />
    <formValue name="password" value="$(Password)" />
</post>

<!-- By form index (0-based) -->
<post formIndex="0">
    <formValue name="search" value="query" />
</post>

<!-- By form name -->
<post formName="contactForm">
    <formValue name="message" value="Hello!" />
</post>
```

### Output Extraction

Extract values from responses using XPath or Regex:

```xml
<!-- XPath extraction -->
<output name="productName" xpath="//div[@class='product-title']" attr="text()" />

<!-- Extract attribute value -->
<output name="imageUrl" xpath="//img[@id='product-image']" attr="src" />

<!-- With expected value validation -->
<output name="status" xpath="//span[@class='status']" attr="text()" expectedValue="Success" />

<!-- Regex extraction -->
<output name="orderId" regex="Order #(\d+)" />

<!-- Multiple values -->
<output name="allPrices" xpath="//span[@class='price']" attr="text()" isMultipleValue="true" />
```

### Conditional Execution

Execute actions conditionally based on parameter values:

```xml
<if property="PaymentMethod" value="CreditCard">
    <post clientID="credit-card-form">
        <formValue name="card_number" value="$(CardNumber)" />
    </post>
</if>
```

### Reusable Targets

Define and call reusable targets:

```xml
<InteractConfig defaultTarget="Main">
    <target name="Login">
        <post clientID="login-form">
            <formValue name="username" value="$(Username)" />
            <formValue name="password" value="$(Password)" />
        </post>
    </target>
    
    <target name="Main">
        <!-- Call the Login target -->
        <call target="Login" />
        
        <!-- Continue with other actions -->
        <get url="http://example.com/dashboard" />
    </target>
</InteractConfig>
```

## 🔧 Configuration Reference

### InteractConfig Attributes

| Attribute | Description |
|-----------|-------------|
| `defaultTarget` | The name of the target to execute by default |

### Target Attributes

| Attribute | Description |
|-----------|-------------|
| `name` | Unique identifier for the target |

### FormValue Attributes

| Attribute | Description |
|-----------|-------------|
| `name` | The form field name |
| `value` | The value to set (supports `$(param)` syntax) |
| `text` | Alternative to `value` for setting field text |

## 🏗️ Architecture

```
NetInteractor
├── Config/           # XML configuration models
├── Interacts/        # Action implementations (Get, Post, If, Call)
├── WebAccessors/     # HTTP client abstractions
├── InteractExecutor  # Main execution engine
└── ConfigFactory     # XML deserialization
```

## 📋 Requirements

- .NET Standard 2.0+ / .NET 8.0+ / .NET 9.0+
- Dependencies:
  - HtmlAgilityPack (HTML parsing)
  - Microsoft.Extensions.Logging (Logging support)
  - Microsoft.Extensions.DependencyInjection (DI support)
- Optional Dependencies (for browser automation):
  - Microsoft.Playwright (NetInteractor.Playwright package)

## 📄 License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📬 Contact

- **Author**: Kerry Jiang
- **Repository**: [https://github.com/kerryjiang/NetInteractor](https://github.com/kerryjiang/NetInteractor)
