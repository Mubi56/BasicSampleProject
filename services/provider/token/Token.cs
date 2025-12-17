namespace Paradigm.Service
{
    public struct Token
    {
        public readonly string access_token;
        public readonly long expires_in;

        public Token(string accessToken, long expiresIn)
        {
            this.access_token = accessToken;
            this.expires_in = expiresIn;
        }
    }
}
