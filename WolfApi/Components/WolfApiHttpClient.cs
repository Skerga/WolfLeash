namespace WolfApi;
public class WolfApiHttpClient(SocketsHttpHandler socketsHttpHandler) : HttpClient(socketsHttpHandler);