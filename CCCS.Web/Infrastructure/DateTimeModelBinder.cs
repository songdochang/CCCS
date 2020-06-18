using System;
using System.Globalization;
using System.Web.Mvc;

public class DateTimeModelBinder : IModelBinder
{
    public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
    {
        ValueProviderResult valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        ModelState modelState = new ModelState { Value = valueResult };
        object actualValue = null;
        try
        {
            if (DateTime.Parse(valueResult.AttemptedValue) < DateTime.Parse("01/01/2000"))
                throw new FormatException();

            actualValue = Convert.ToDateTime(valueResult.AttemptedValue, CultureInfo.CurrentCulture);
        }
        catch (FormatException e)
        {
            modelState.Errors.Add(e);
        }

        bindingContext.ModelState.Add(bindingContext.ModelName, modelState);
        return actualValue;
    }
}

