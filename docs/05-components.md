# Building shared components

Users of the pizza store can now track the status of their orders in real time. In this session we'll build out a component library that advanced provides for use in other projects. We'll also use JavaScript interop to add a real-time map to the order status page that answers the age old question, "Where's my pizza?!?".

## The Map component

Included in the ComponentsLibrary project is a prebuilt `Map` component for displaying the location of a set of markers and animating their movements over time. We'll use this component to show the location of the user's pizza orders as they are being delivered, but first let's look at how the `Map` component is implemented.

Open *Map.razor* and take a look at the code:

```csharp
@using Microsoft.JSInterop
@inject IJSRuntime JSRuntime

<div id="@elementId" style="height: 100%; width: 100%;"></div>

@code {
    string elementId = $"map-{Guid.NewGuid().ToString("D")}";
    
    [Parameter] double Zoom { get; set; }
    [Parameter, EditorRequired] public List<Marker> Markers { get; set; } = new();

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        await JSRuntime.InvokeVoidAsync(
            "deliveryMap.showOrUpdate",
            elementId,
            Markers);
    }
}
```

The `Map` component uses dependency injection to get an `IJSRuntime` instance. This service can be used to make JavaScript calls to browser APIs or existing JavaScript libraries by calling the `InvokeVoidAsync` or `InvokeAsync<TResult>` method. The first parameter to this method specifies the path to the JavaScript function to call relative to the root `window` object. The remaining parameters are arguments to pass to the JavaScript function. The arguments are serialized to JSON so they can be handled in JavaScript.

The `Map` component first renders a `div` with a unique ID for the map and then calls the `deliveryMap.showOrUpdate` function to display the map in the specified element with the specified markers passed to the `Map` component. This is done in the `OnAfterRenderAsync` component lifecycle event to ensure that the component is done rendering its markup. The `deliveryMap.showOrUpdate` function is defined in the *wwwroot/deliveryMap.js* file, which then uses [leaflet.js](http://leafletjs.com) and [OpenStreetMap](https://www.openstreetmap.org/) to display the map. The details of how this code works isn't really important - the critical point is that it's possible to call any JavaScript function this way.

How do these files make their way to the Blazor app? For a Blazor library project (using `Sdk="Microsoft.NET.Sdk.Razor"`) any files in the `wwwroot/` folder will be bundled with the library. The server project will automatically serve these files using the static files middleware.

The final link is for the page hosting the Blazor client app to include the desired files (in our case `.js` and `.css`). The `BlazingPizza/App.razor` includes these files using relative URIs like `_content/BlazingPizza.ComponentsLibrary/localStorage.js`. This is the general pattern for references files bundled with a Blazor class library - `_content/<library name>/<file path>`.

---

If you start typing in `Map`, you'll notice that the editor doesn't offer completion for it. This is because the binding between elements and components are governed by C#'s namespace binding rules. The `Map` component is defined in the `BlazingPizza.ComponentsLibrary.Map` namespace, which we don't have an `@using` for.

Add an `@using` for this namespace to the root `_Imports.razor` to bring this component into scope:
```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.JSInterop
@using BlazingPizza.Client
@using BlazingPizza.Client.Shared
@using BlazingPizza.ComponentsLibrary
@using BlazingPizza.ComponentsLibrary.Map
```

Add the `Map` component to the `OrderDetails` page by adding the following just below the `track-order-details` `div`:

```html
<div class="track-order-map">
    <Map Zoom="13" Markers="orderWithStatus.MapMarkers" />
</div>
```

*The reason why we haven't needed to add `@using`s for our components before now is that our root `_Imports.razor` already contains an `@using BlazingPizza.Shared` which matches the reusable components we have written.*

When the `OrderDetails` component polls for order status updates, an update set of markers is returned with the latest location of the pizzas, which then gets reflected on the map.

![Real-time pizza map](https://user-images.githubusercontent.com/1874516/51807322-6018b880-227d-11e9-89e5-ef75f03466b9.gif)

## Add a confirm prompt for deleting pizzas

The JavaScript interop code for the `Map` component was provided for you. Next you'll add some JavaScript interop code of your own.

It would be a shame if users accidentally deleted pizzas from their order (and ended up not buying them!). Let's add a confirm prompt when the user tries to delete a pizza. We'll show the confirm prompt using JavaScript interop.

Add a static `JSRuntimeExtensions` class to the Client project with a `Confirm` extension method off of `IJSRuntime`. Implement the `Confirm` method to call the built-in JavaScript `confirm` function.

```csharp
public static class JSRuntimeExtensions
{
    public static ValueTask<bool> Confirm(this IJSRuntime jsRuntime, string message)
    {
        return jsRuntime.InvokeAsync<bool>("confirm", message);
    }
}
```

Inject the `IJSRuntime` service into the `BlazingPizza.Client/Components/Pages/Home.razor` component so that it can be used there to make JavaScript interop calls.

```razor
@page "/"
@rendermode InteractiveWebAssembly
@inject IRepository PizzaStore
@inject OrderState OrderState
@inject NavigationManager NavigationManager
@inject IJSRuntime JSRuntime
```

Add an async `RemovePizza` method to the `Home` component that calls the `Confirm` method to verify if the user really wants to remove the pizza from the order.

```csharp
async Task RemovePizza(Pizza configuredPizza)
{
    if (await JS.Confirm($"Remove {configuredPizza.Special?.Name} pizza from the order?"))
    {
        OrderState.RemoveConfiguredPizza(configuredPizza);
    }
}
```

In the `Home` component update the event handler for the `ConfiguredPizzaItems` to call the new `RemovePizza` method. 

```csharp
@foreach (var configuredPizza in OrderState.Order.Pizzas)
{
    <ConfiguredPizzaItem Pizza="configuredPizza" OnRemoved="@(() => RemovePizza(configuredPizza))" />
}
```

Run the app and try removing a pizza from the order.

![Confirm pizza removal](https://user-images.githubusercontent.com/1874516/77243688-34b40400-6bca-11ea-9d1c-331fecc8e307.png)

Notice that we didn't have to update the signature of `ConfiguredPizzaItem.OnRemoved` to support async. This is another special property of `EventCallback`, it supports both synchronous event handlers and asynchronous event handlers.

## Templated components

Let's refactor some of the original components and make them more reusable. Along the way we'll also create a separate library project as a home for the new components.

This project already has a components library, **BlazingPizza.ComponentsLibrary**  If we needed to create a new library for sharing a different set of components, we could follow these instructions:

## Creating a component library (Visual Studio)

Using Visual Studio, right click on `Solution` at the very top of solution explorer, and choose `Add->New Project`. 

Then, select the Razor Class Library template.

![image](https://user-images.githubusercontent.com/1430011/65823337-17990c80-e209-11e9-9096-de4cb0d720ba.png)

Enter the project name `BlazingComponents` and click *Create*.

## Creating a component library (command line)

To make a new project using **dotnet** run the following commands from the directory where your solution file exists.

```
dotnet new razorclasslib -o BlazingComponents
dotnet sln add BlazingComponents
```

This should create a new project called `BlazingComponents` and add it to the solution file.

## Understanding the library project

Open the project file by double-clicking on the *BlazingPizza.ComponentsLibrary* project name in *Solution explorer*. We're not going to modify anything here, but it would be good to understand a few things.

It looks like:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net{versionNumber}</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <SupportedPlatform Include="browser" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="versionNumber" />
  </ItemGroup>

</Project>

```

There are a few things here worth understanding. 

Firstly, the package targets a version of .NET, ex: `<TargetFramework>net8.0</TargetFramework>`.

Additional, the `<SupportedPlatform Include="browser"/>3.0</RazorLangVersion>` identifies the supported platforms. This value is understood by the compatibility analyzer. Client-side apps target the full .NET API surface area, but not all .NET APIs are supported on WebAssembly due to browser sandbox constraints. Unsupported APIs throw PlatformNotSupportedException when running on WebAssembly. A platform compatibility analyzer warns the developer when the app uses APIs that aren't supported by the app's target platforms.  

Lastly the `<PackageReference />` element adds a package references to the Blazor component model.

## Writing a templated dialog

We are going to revisit the dialog system that is part of `Home` and turn it into something that's decoupled from the application.

Let's think about how a *reusable dialog* should work. We would expect a dialog component to handle showing and hiding itself, as well as maybe styling to appear visually as a dialog. However, to be truly reusable, we need to be able to provide the content for the inside of the dialog. We call a component that accepts *content* as a parameter a *templated component*.

Blazor happens to have a feature that works for exactly this case, and it's similar to how a layout works. Recall that a layout has a `Body` parameter, and the layout gets to place other content *around* the `Body`. In a layout, the `Body` parameter is of type `RenderFragment` which is a delegate type that the runtime has special handling for. The good news is that this feature is not limited to layouts. Any component can declare a parameter of type `RenderFragment`. We've also used this feature extensively in `Routes.razor`. All of the components used to handle routing and authorization are templated components.

Let's get started on this new dialog component. Create a new component file named `TemplatedDialog.razor` in the `BlazingComponents` project. Put the following markup inside `TemplatedDialog.razor`:

```html
<div class="dialog-container">
    <div class="dialog">

    </div>
</div>
```

This doesn't do anything yet because we haven't added any parameters. Recall from before the two things we want to accomplish.

1. Accept the content of the dialog as a parameter
2. Render the dialog conditionally if it is supposed to be shown

First, add a parameter called `ChildContent` of type `RenderFragment`. The name `ChildContent` is a special parameter name, and is used by convention when a component wants to accept a single content parameter.

```razor
@code {
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }
}
```

Next, update the markup to *render* the `ChildContent` in the middle of the markup. It should look like this:

```html
<div class="dialog-container">
    <div class="dialog">
        @ChildContent
    </div>
</div>
```

If this structure looks weird to you, cross-check it with your layout file, which follows a similar pattern. Even though `RenderFragment` is a delegate type, the way to *render* it is not by invoking it, it's by placing the value in a normal expression so the runtime may invoke it.

Next, to give this dialog some conditional behavior, let's add a parameter of type `bool` called `Show`. After doing that, it's time to wrap all of the existing content in an `@if (Show) { ... }`. The full file should look like this:

```html
@if (Show)
{
    <div class="dialog-container">
        <div class="dialog">
            @ChildContent
        </div>
    </div>
}

@code {
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool Show { get; set; }
}
```

Build the solution and make sure that everything compiles at this stage. Next we'll get down to using this new component.

## Adding a reference to the templated library

Before we can use this component in the `BlazingPizza.Client` project, we will need to add a project reference. Do this by adding a project reference from `BlazingPizza.Client` to `BlazingPizza.ComponentsLibrary`.

Once that's done, there's one more minor step. Open the `_Imports.razor` in the topmost directory of `BlazingPizza.Client` and add this line at the end:

```html
@using BlazingPizza.ComponentsLibrary
```

Now that the project reference has been added, do a build again to verify that everything still compiles.

## Another refactor

Recall that our `TemplatedDialog` contains a few `div`s. Well, this duplicates some of the structure of `ConfigurePizzaDialog`. Let's clean that up. Open `ConfigurePizzaDialog.razor`; it currently looks like:

```html
<div class="dialog-container">
    <div class="dialog">
        <div class="dialog-title">
        ...
        </div>
        <form class="dialog-body">
        ...
        </form>

        <div class="dialog-buttons">
        ...
        </div>
    </div>
</div>
```

We should remove the outermost two layers of `div` elements since those are now part of the `TemplatedDialog` component. After removing these it should look more like:

```html
<div class="dialog-title">
...
</div>
<form class="dialog-body">
...
</form>

<div class="dialog-buttons">
...
</div>
```

## Using the new dialog

We'll use this new templated component from `Home.razor`. Open `Home.razor` and find the block of code that looks like:

```html
@if (OrderState.ShowingConfigureDialog)
{
    <ConfigurePizzaDialog
        Pizza="OrderState.ConfiguringPizza"
        OnConfirm="OrderState.ConfirmConfigurePizzaDialog"
        OnCancel="OrderState.CancelConfigurePizzaDialog" />
}
```

We are going to remove this and replace it with an invocation of the new component. Replace the block above with code like the following:

```html
<TemplatedDialog Show="OrderState.ShowingConfigureDialog">
    <ConfigurePizzaDialog 
        Pizza="OrderState.ConfiguringPizza" 
        OnCancel="OrderState.CancelConfigurePizzaDialog" 
        OnConfirm="OrderState.ConfirmConfigurePizzaDialog" />
</TemplatedDialog>
```

This is wiring up our new `TemplatedDialog` component to show and hide itself based on `OrderState.ShowingConfigureDialog`. Also, we're passing in some content to the `ChildContent` parameter. Since we called the parameter `ChildContent` any content that is placed inside the `<TemplatedDialog> </TemplatedDialog>` will be captured by a `RenderFragment` delegate and passed to `TemplatedDialog`. 

> Note: A templated component may have multiple `RenderFragment` parameters. What we're showing here is a convenient convention when the caller wants to provide a single `RenderFragment` that represents the *main* content.

At this point it should be possible to run the code and see that the new dialog works correctly. Verify that this is working correctly before moving on to the next step.

## A more advanced templated component

Now that we've done a basic templated dialog, we're going to try something more sophisticated. Recall that the `MyOrders.razor` page shows a list of orders, but it also contains three-state logic (loading, empty list, and showing items). If we could extract that logic into a reusable component, would that be useful? Let's give it a try.

Start by creating a new file `TemplatedList.razor` in the `BlazingComponents` project. We want this list to have a few features:
1. Async-loading of any type of data
2. Separate rendering logic for three states - loading, empty list, and showing items

We can solve async loading by accepting a delegate of type `Func<Task<IEnumerable<?>>>` - we need to figure out what type should replace **?**. Since we want to support any kind of data, we need to declare this component as a generic type. We can make a generic-typed component using the `@typeparam` directive, so place this at the top of `TemplatedList.razor`.

```html
@typeparam TItem
```

Making a generic-typed component works similarly to other generic types in C#, in fact `@typeparam` is just a convenient Razor syntax for a generic .NET type.

> Note: We don't yet have support for type-parameter-constraints. This is something we're looking to add in the future.

Now that we've defined a generic type parameter we can use it in a parameter declaration. Let's add a parameter to accept a delegate we can use to load data, and then load the data in a similar fashion to our other components.

```html
@code {
    IEnumerable<TItem>? items;

    [Parameter, EditorRequired] public Func<Task<IEnumerable<TItem>>>? Loader { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (Loader is not null)
        {
            items = await Loader();
        }
    }
}
```

Since we have the data, we can now add the structure of each of the states we need to handle. Add the following markup to `TemplatedList.razor`:

```html
@if (items is null)
{

}
else if (!items.Any())
{
}
else
{
    <div class="list-group">
        @foreach (var item in items)
        {
            <div class="list-group-item">
                
            </div>
        }
    </div>
}
```

Now, these are our three states of the dialog, and we'd like to accept a content parameter for each one so the caller can plug in the desired content. We do this by defining three `RenderFragment` parameters. Since we have multiple `RenderFragment` parameters we'll just give each one their own descriptive names instead of calling them `ChildContent`. The content for showing an item needs to take a parameter. We can do this by using `RenderFragment<T>`.

Here's an example of the three parameters to add:

```C#
    [Parameter] public RenderFragment? Loading { get; set; }
    [Parameter] public RenderFragment? Empty { get; set; }
    [Parameter, EditorRequired] public RenderFragment<TItem>? Item { get; set; }
```

Now that we have some `RenderFragment` parameters, we can start using them. Update the markup we created earlier to plug in the correct parameter in each place.

```html
@if (items is null)
{
    @Loading
}
else if (!items.Any())
{
    @Empty
}
else
{
    <div class="list-group">
        @foreach (var item in items)
        {
            <div class="list-group-item">
                @if (Item is not null)
                {
                    @Item(item)
                }
            </div>
        }
    </div>
}
```

The `Item` accepts a parameter, and the way to deal with this is just to invoke the function. The result of invoking a `RenderFragment<T>` is another `RenderFragment` which can be rendered directly.

The new component should compile at this point, but there's still one thing we want to do. We want to be able to style the `<div class="list-group">` with another class, since that's what `MyOrders.razor` is doing. Adding small extensibiliy points to plug in additional css classes can go a long way for reusability.

Let's add another `string` parameter, and finally the functions block of `TemplatedList.razor` should look like:

```html
@code {
    IEnumerable<TItem>? items;

    [Parameter, EditorRequired] public Func<Task<IEnumerable<TItem>>>? Loader { get; set; }
    [Parameter] public RenderFragment? Loading { get; set; }
    [Parameter] public RenderFragment? Empty { get; set; }
    [Parameter, EditorRequired] public RenderFragment<TItem>? Item { get; set; }
    [Parameter] public string? ListGroupClass { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (Loader is not null)
        {
            items = await Loader();
        }
    }
}
```

Lastly update the `<div class="list-group">` to contain `<div class="list-group @ListGroupClass">`. The complete file of `TemplatedList.razor` should now look like:

```html
@typeparam TItem

@if (items is null)
{
    @Loading
}
else if (!items.Any())
{
    @Empty
}
else
{
    <div class="list-group @ListGroupClass">
        @foreach (var item in items)
        {
            <div class="list-group-item">
                @if (Item is not null)
                {
                    @Item(item)
                }
            </div>
        }
    </div>
}

@code {
    IEnumerable<TItem>? items;

    [Parameter, EditorRequired] public Func<Task<IEnumerable<TItem>>>? Loader { get; set; }
    [Parameter] public RenderFragment? Loading { get; set; }
    [Parameter] public RenderFragment? Empty { get; set; }
    [Parameter, EditorRequired] public RenderFragment<TItem>? Item { get; set; }
    [Parameter] public string? ListGroupClass { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (Loader is not null)
        {
            items = await Loader();
        }
    }
}
```

## Using TemplatedList

To use the new `TemplatedList` component, we're going to edit `MyOrders.razor`.

First, we need to create a delegate that we can pass to the `TemplatedList` that will load order data. We can do this by keeping the code that's in `MyOrders.OnParametersSetAsync` and changing the method signature. The `@code` block should look something like:

```html
@code {
    async Task<IEnumerable<OrderWithStatus>> LoadOrders()
    {
        var ordersWithStatus = Enumerable.Empty<OrderWithStatus>();
        try
        {
            ordersWithStatus = await OrdersClient.GetOrders();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
        }
        return ordersWithStatus;
    }
}
```

This matches the signature expected by the `Loader` parameter of `TemplatedList`, it's a `Func<Task<IEnumerable<?>>>` where the **?** is replaced with `OrderWithStatus` so we are on the right track.

You can use the `TemplatedList` component now like so:

```html
<div class="main">
    <TemplatedList>
    </TemplatedList>
</div>
```

The compiler will complain about not knowing the generic type of `TemplatedList`. The compiler is smart enough to perform type inference like normal C# but we haven't given it enough information to work with.

Adding the `Loader` attribute should fix the issue.

```html
<div class="main">
    <TemplatedList Loader="LoadOrders">
    </TemplatedList>
</div>
```

> Note: A generic-typed component can have its type-parameters manually specified as well by setting the attribute with a matching name to the type parameter - in this case it's called `TItem`. There are some cases where this is necessary so it's worth knowing.

```html
<div class="main">
    <TemplatedList TItem="OrderWithStatus">
    </TemplatedList>
</div>
```

We don't need to do this right now because the type can be inferred from `Loader`.

-----

Next, we need to think about how to pass multiple content (`RenderFragment`) parameters to a component. We've learned using `TemplatedDialog` that a single `[Parameter] RenderFragment ChildContent` can be set by nesting content inside the component. However this is just a convenient syntax for the most simple case. When you want to pass multiple content parameters, you can do this by nesting elements inside the component that match the parameter names.

For our `TemplatedList` here's an example that sets each parameter to some dummy content:

```html
<div class="main">
    <TemplatedList Loader="LoadOrders">
        <Loading>Hi there!</Loading>
        <Empty>
            How are you?
        </Empty>
        <Item>
            Are you enjoying Blazor?
        </Item>
    </TemplatedList>
</div>
```

The `Item` parameter is a `RenderFragment<T>` - which accepts a parameter. By default this parameter is called `context`. If we type inside of `<Item>  </Item>` then it should be possible to see that `@context` is bound to a variable of type `OrderStatus`. We can rename the parameter by using the `Context` attribute:

```html
<div class="main">
    <TemplatedList Loader="LoadOrders">
        <Loading>Hi there!</Loading>
        <Empty>
            How are you?
        </Empty>
        <Item Context="item">
            Are you enjoying Blazor?
        </Item>
    </TemplatedList>
</div>
```

Now we want to include all of the existing content from `MyOrders.razor`, so putting it all together should look more like the following:

```html
<div class="main">
    <TemplatedList Loader="LoadOrders" ListGroupClass="orders-list">
        <Loading>Loading...</Loading>
        <Empty>
            <h2>No orders placed</h2>
            <a class="btn btn-success" href="">Order some pizza</a>
        </Empty>
        <Item Context="item">
            <div class="col">
                <h5>@item.Order.CreatedTime.ToLongDateString()</h5>
                Items:
                <strong>@item.Order.Pizzas.Count()</strong>;
                Total price:
                <strong>£@item.Order.GetFormattedTotalPrice()</strong>
            </div>
            <div class="col">
                Status: <strong>@item.StatusText</strong>
            </div>
            <div class="col flex-grow-0">
                <a href="myorders/@item.Order.OrderId" class="btn btn-success">
                    Track &gt;
                </a>
            </div>
        </Item>
    </TemplatedList>
</div>
```

Notice that we're also setting the `ListGroupClass` parameter to add the additional styling that was present in the original `MyOrders.razor`. 

There were a number of steps and new features to introduce here. Run this and make sure that it works correctly now that we're using the templated list.

To prove that the list is really working correctly we can try the following: 
1. Delete the `pizza.db` from the `Blazor.Server` project to test the case where there are no orders
2. Add an `await Task.Delay(3000);` to `LoadOrders` (also marking that method as `async`) to test the case where we're still loading

## Summary

So what have we seen in this session?

1. It's possible to write components that accept *content* as a parameter - even multiple content parameters
2. Templated components can be used to abstract things, like showing a dialog, or async loading of data
3. Components can be generic types, which makes them more reusable
