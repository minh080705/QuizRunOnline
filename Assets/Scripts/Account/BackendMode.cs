/// <summary>
/// Bản build Windows standalone không dùng Firebase (SDK desktop không ổn định, game nhắm tới Android):
/// đăng nhập và hồ sơ chạy cục bộ trong bộ nhớ. Editor và Android dùng Firebase thật.
/// </summary>
public static class BackendMode
{
#if UNITY_STANDALONE && !UNITY_EDITOR
    public static readonly bool UseFirebase = false;
#else
    public static readonly bool UseFirebase = true;
#endif
}
