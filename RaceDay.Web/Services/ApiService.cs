using System.Net.Http.Headers;

public class ApiService
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _accessor;

    public ApiService(IHttpClientFactory factory, IHttpContextAccessor accessor)
    {
        _factory = factory;
        _accessor = accessor;
    }

    public HttpClient CreateClient()
    {
        var client = _factory.CreateClient("RaceDayApi");
        var token = _accessor.HttpContext?.Session.GetString("Token");

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}