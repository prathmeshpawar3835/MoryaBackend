using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GramShopPOS.API.Filters;

public sealed class ApiResponseWrapFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: FileDownload download })
        {
            context.Result = new FileContentResult(download.Content, download.ContentType)
            {
                FileDownloadName = download.FileName
            };
        }
        else if (context.Result is ObjectResult objectResult && objectResult.Value is not ApiResponse and not ApiResponse<object>
                 and not null && objectResult.Value.GetType().IsGenericType && objectResult.Value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            // already wrapped
        }
        else if (context.Result is ObjectResult { Value: not null } result
                 && result.Value.GetType().Name != "ApiResponse`1"
                 && result.Value is not ProblemDetails)
        {
            var message = context.HttpContext.Request.Method switch
            {
                "POST" => "Created successfully.",
                "PUT" => "Updated successfully.",
                "DELETE" => "Deleted successfully.",
                _ => "Success."
            };
            var wrapperType = typeof(ApiResponse<>).MakeGenericType(result.Value.GetType());
            var wrapped = Activator.CreateInstance(wrapperType);
            wrapperType.GetProperty(nameof(ApiResponse<object>.Success))!.SetValue(wrapped, true);
            wrapperType.GetProperty(nameof(ApiResponse<object>.Message))!.SetValue(wrapped, message);
            wrapperType.GetProperty(nameof(ApiResponse<object>.Data))!.SetValue(wrapped, result.Value);
            result.Value = wrapped;
        }

        await next();
    }
}
