namespace BigBang1112.WorldRecordReport.Exceptions;

[Serializable]
public class ThisShouldNotHappenException : Exception
{
    public ThisShouldNotHappenException() { }
    public ThisShouldNotHappenException(string message) : base(message) { }
    public ThisShouldNotHappenException(string message, Exception inner) : base(message, inner) { }
    protected ThisShouldNotHappenException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
