# AutoConstructor

[![NuGet](https://img.shields.io/nuget/v/AutoConstructor.svg)](https://www.nuget.org/packages/AutoConstructor/)
[![GitHub release](https://img.shields.io/github/release/k94ll13nn3/AutoConstructor.svg)](https://github.com/k94ll13nn3/AutoConstructor/releases/latest)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/k94ll13nn3/AutoConstructor/main/LICENSE)
![ci.yml](https://github.com/k94ll13nn3/AutoConstructor/workflows/.github/workflows/ci.yml/badge.svg)

C# source generator that generates a constructor from readonly fields in a class.

## Installation

- Grab the latest package on [NuGet](https://www.nuget.org/packages/AutoConstructor/).

## Requirements

- Visual Studio 16.9+ is needed
- .NET SDK 5.0.300+ is needed

## How to use

For any class where the generator will be used:

- Mark the class as `partial`
- Use `AutoConstructorAttribute` on the class

By default, all `private readonly` without initialization will be used. The will be injected with the same name without any leading `_`.

Fields marked with `AutoConstructorIgnoreAttribute` will be ignored.

Use `AutoConstructorInjectAttribute` to customize the behavior, usualy when the injected parameter and the fields
do not have the same type. It takes three parameters:

- `Initializer`: a string that will be used to initialize the field (by example `myService.GetData()`)
- `ParameterName`: the name of the parameter to used in the constructor  (by example `myService`)
- `InjectedType`: the type of the parameter to used in the constructor  (by example `IMyService`)

When using `AutoConstructorInjectAttribute`, the parameter name can be shared across multiple fields,
and even use a parameter from another field not annotated with `AutoConstructorInjectAttribute`, but type must match.

## Configuration

By default, null checks with `ArgumentNullException` will be generated when needed.
To disable this behavior, set `AutoConstructor_DisableNullChecking` to `false` in the project file:

``` xml
<AutoConstructor_DisableNullChecking>true</AutoConstructor_DisableNullChecking>
```

## Sample

The following code

``` csharp
[AutoConstructor]
partial class Test
{
    private readonly string _name;

    // Won't be injected
    private readonly Uri _uri = new Uri("/non-modified", UriKind.Relative);

    // Won't be injected
    [AutoConstructorIgnore]
    private readonly DateTime _dateNotTaken;

    // Won't be injected
    private int? _toto;

    // Support for nullables
    private readonly DateTime? _date;

    // Support for generics
    private readonly List<DateTime> _items;

    // Inject with custom initializer
    [AutoConstructorInject("guid.ToString()", "guid", typeof(Guid))]
    private readonly string _guidString;

    // Use existing parameter defined with AutoConstructorInject
    [AutoConstructorInject("guid.ToString().Length", "guid", typeof(Guid))]
    private readonly int _guidLength;

    // Use existing parameter from a basic injection
    [AutoConstructorInject("name.ToUpper()", "name", typeof(string))]
    private readonly string _nameShared;
}
```

will generate

```csharp
public Test(string name, System.DateTime? date, System.Collections.Generic.List<System.DateTime> items, System.Guid guid)
{
    this._name = name ?? throw new System.ArgumentNullException(nameof(name));
    this._date = date ?? throw new System.ArgumentNullException(nameof(date));
    this._items = items ?? throw new System.ArgumentNullException(nameof(items));
    this._guidString = guid.ToString() ?? throw new System.ArgumentNullException(nameof(guid));
    this._guidLength = guid.ToString().Length;
    this._nameShared = name.ToUpper() ?? throw new System.ArgumentNullException(nameof(name));
}
```

## Diagnostics

### ACONS01

The `AutoConstructor` attribute is used on a class that is not partial.

### ACONS02

The `AutoConstructor` attribute is used on a class without fiels to inject.

### ACONS03

The `AutoConstructorIgnore` attribute is used on a field that won't already be processed.

### ACONS04

The `AutoConstructorInject` attribute is used on a field that won't already be processed.

### ACONS05

The `AutoConstructorIgnore` or `AutoConstructorInject` are used on a class without the `AutoConstructor` attribute.
