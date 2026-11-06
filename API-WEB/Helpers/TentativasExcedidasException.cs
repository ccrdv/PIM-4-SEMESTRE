namespace SistemaWhiteLabel.API.Helpers
{
    public class TentativasExcedidasException : Exception
    {
        public TentativasExcedidasException(string mensagem) : base(mensagem) { }
    }
}
