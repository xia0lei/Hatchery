// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: TestServer.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace TestServer {

  /// <summary>Holder for reflection information generated from TestServer.proto</summary>
  public static partial class TestServerReflection {

    #region Descriptor
    /// <summary>File descriptor for TestServer.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static TestServerReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChBUZXN0U2VydmVyLnByb3RvEgpUZXN0U2VydmVyIkIKFFRlc3RTZXJ2ZXJf",
            "T25SZXF1ZXN0EhQKDHJlcXVlc3RfdGltZRgBIAEoAxIUCgxyZXF1ZXN0X3Rl",
            "eHQYAiABKAkiRQoVVGVzdFNlcnZlcl9PblJlc3BvbnNlEhUKDXJlc3BvbnNl",
            "X3RpbWUYASABKAMSFQoNcmVzcG9uc2VfdGV4dBgCIAEoCWIGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::TestServer.TestServer_OnRequest), global::TestServer.TestServer_OnRequest.Parser, new[]{ "RequestTime", "RequestText" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::TestServer.TestServer_OnResponse), global::TestServer.TestServer_OnResponse.Parser, new[]{ "ResponseTime", "ResponseText" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class TestServer_OnRequest : pb::IMessage<TestServer_OnRequest>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<TestServer_OnRequest> _parser = new pb::MessageParser<TestServer_OnRequest>(() => new TestServer_OnRequest());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<TestServer_OnRequest> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::TestServer.TestServerReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestServer_OnRequest() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestServer_OnRequest(TestServer_OnRequest other) : this() {
      requestTime_ = other.requestTime_;
      requestText_ = other.requestText_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestServer_OnRequest Clone() {
      return new TestServer_OnRequest(this);
    }

    /// <summary>Field number for the "request_time" field.</summary>
    public const int RequestTimeFieldNumber = 1;
    private long requestTime_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long RequestTime {
      get { return requestTime_; }
      set {
        requestTime_ = value;
      }
    }

    /// <summary>Field number for the "request_text" field.</summary>
    public const int RequestTextFieldNumber = 2;
    private string requestText_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string RequestText {
      get { return requestText_; }
      set {
        requestText_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as TestServer_OnRequest);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(TestServer_OnRequest other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (RequestTime != other.RequestTime) return false;
      if (RequestText != other.RequestText) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (RequestTime != 0L) hash ^= RequestTime.GetHashCode();
      if (RequestText.Length != 0) hash ^= RequestText.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (RequestTime != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(RequestTime);
      }
      if (RequestText.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(RequestText);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (RequestTime != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(RequestTime);
      }
      if (RequestText.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(RequestText);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (RequestTime != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(RequestTime);
      }
      if (RequestText.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(RequestText);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(TestServer_OnRequest other) {
      if (other == null) {
        return;
      }
      if (other.RequestTime != 0L) {
        RequestTime = other.RequestTime;
      }
      if (other.RequestText.Length != 0) {
        RequestText = other.RequestText;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            RequestTime = input.ReadInt64();
            break;
          }
          case 18: {
            RequestText = input.ReadString();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 8: {
            RequestTime = input.ReadInt64();
            break;
          }
          case 18: {
            RequestText = input.ReadString();
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class TestServer_OnResponse : pb::IMessage<TestServer_OnResponse>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<TestServer_OnResponse> _parser = new pb::MessageParser<TestServer_OnResponse>(() => new TestServer_OnResponse());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<TestServer_OnResponse> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::TestServer.TestServerReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestServer_OnResponse() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestServer_OnResponse(TestServer_OnResponse other) : this() {
      responseTime_ = other.responseTime_;
      responseText_ = other.responseText_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestServer_OnResponse Clone() {
      return new TestServer_OnResponse(this);
    }

    /// <summary>Field number for the "response_time" field.</summary>
    public const int ResponseTimeFieldNumber = 1;
    private long responseTime_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long ResponseTime {
      get { return responseTime_; }
      set {
        responseTime_ = value;
      }
    }

    /// <summary>Field number for the "response_text" field.</summary>
    public const int ResponseTextFieldNumber = 2;
    private string responseText_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string ResponseText {
      get { return responseText_; }
      set {
        responseText_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as TestServer_OnResponse);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(TestServer_OnResponse other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (ResponseTime != other.ResponseTime) return false;
      if (ResponseText != other.ResponseText) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (ResponseTime != 0L) hash ^= ResponseTime.GetHashCode();
      if (ResponseText.Length != 0) hash ^= ResponseText.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (ResponseTime != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(ResponseTime);
      }
      if (ResponseText.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(ResponseText);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (ResponseTime != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(ResponseTime);
      }
      if (ResponseText.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(ResponseText);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (ResponseTime != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(ResponseTime);
      }
      if (ResponseText.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(ResponseText);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(TestServer_OnResponse other) {
      if (other == null) {
        return;
      }
      if (other.ResponseTime != 0L) {
        ResponseTime = other.ResponseTime;
      }
      if (other.ResponseText.Length != 0) {
        ResponseText = other.ResponseText;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            ResponseTime = input.ReadInt64();
            break;
          }
          case 18: {
            ResponseText = input.ReadString();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 8: {
            ResponseTime = input.ReadInt64();
            break;
          }
          case 18: {
            ResponseText = input.ReadString();
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code
