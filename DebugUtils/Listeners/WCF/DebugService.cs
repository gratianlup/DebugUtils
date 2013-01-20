// Copyright (c) Gratian Lup. All rights reserved.
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following
//       disclaimer in the documentation and/or other materials provided
//       with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

namespace DebugUtils.Debugger.Listeners.WCF {
    using System;
    using System.Runtime.Serialization;


    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "3.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "ClientResponse", Namespace = "http://schemas.datacontract.org/2004/07/DebugViewerControl")]
    internal enum ClientResponse : int {

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Continue = 0,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Break = 1,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Exit = 2,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "3.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "EventType", Namespace = "http://schemas.datacontract.org/2004/07/DebugViewerControl")]
    internal enum EventType : int {

        [System.Runtime.Serialization.EnumMemberAttribute()]
        RequestApplicationInfo = 1,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        ExitApplication = 2,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        All = 3,
    }

    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "3.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name = "AppInfo", Namespace = "http://schemas.datacontract.org/2004/07/DebugViewerControl")]
    internal partial class AppInfo : object, System.Runtime.Serialization.IExtensibleDataObject {

        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;

        private string MachineNameField;

        private string MainWindowTitleField;

        private long PrivateMemorySizeField;

        private System.DateTime StartTimeField;

        private int ThreadNumberField;

        private System.TimeSpan TotalProcessorTimeField;

        private string UserNameField;

        private System.TimeSpan UserProcessorTimeField;

        private long VirtualMemorySizeField;

        private long WorkingSetField;

        private string WorkingDirectoryField;

        private OperatingSystem OSVersionField;

        private int ProcessorCountField;

        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public string MachineName {
            get {
                return this.MachineNameField;
            }
            set {
                this.MachineNameField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public string MainWindowTitle {
            get {
                return this.MainWindowTitleField;
            }
            set {
                this.MainWindowTitleField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public long PrivateMemorySize {
            get {
                return this.PrivateMemorySizeField;
            }
            set {
                this.PrivateMemorySizeField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public System.DateTime StartTime {
            get {
                return this.StartTimeField;
            }
            set {
                this.StartTimeField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public int ThreadNumber {
            get {
                return this.ThreadNumberField;
            }
            set {
                this.ThreadNumberField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public System.TimeSpan TotalProcessorTime {
            get {
                return this.TotalProcessorTimeField;
            }
            set {
                this.TotalProcessorTimeField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public string UserName {
            get {
                return this.UserNameField;
            }
            set {
                this.UserNameField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public System.TimeSpan UserProcessorTime {
            get {
                return this.UserProcessorTimeField;
            }
            set {
                this.UserProcessorTimeField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public long VirtualMemorySize {
            get {
                return this.VirtualMemorySizeField;
            }
            set {
                this.VirtualMemorySizeField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public long WorkingSet {
            get {
                return this.WorkingSetField;
            }
            set {
                this.WorkingSetField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public string WorkingDirectory {
            get {
                return this.WorkingDirectoryField;
            }
            set {
                this.WorkingDirectoryField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public OperatingSystem OSVersion {
            get {
                return this.OSVersionField;
            }
            set {
                this.OSVersionField = value;
            }
        }

        [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
        public int ProcessorCount {
            get {
                return this.ProcessorCountField;
            }
            set {
                this.ProcessorCountField = value;
            }
        }
    }
}


[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
[System.ServiceModel.ServiceContractAttribute(ConfigurationName = "IDebugService", CallbackContract = typeof(IDebugServiceCallback))]
internal interface IDebugService {

    [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IDebugService/Open", ReplyAction = "http://tempuri.org/IDebugService/OpenResponse")]
    DebugUtils.Debugger.Listeners.WCF.ClientResponse Open(string clientName, string machineName, string processName);

    [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IDebugService/HandleDebugMessage", ReplyAction = "http://tempuri.org/IDebugService/HandleDebugMessageResponse")]
    DebugUtils.Debugger.Listeners.WCF.ClientResponse HandleDebugMessage(string clientName, DebugUtils.Debugger.DebugMessage message);

    [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IDebugService/Close", ReplyAction = "http://tempuri.org/IDebugService/CloseResponse")]
    void Close(string clientName);

    [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IDebugService/Subscribe", ReplyAction = "http://tempuri.org/IDebugService/SubscribeResponse")]
    void Subscribe(string clientName, DebugUtils.Debugger.Listeners.WCF.EventType mask);

    [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IDebugService/Unsubscribe", ReplyAction = "http://tempuri.org/IDebugService/UnsubscribeResponse")]
    void Unsubscribe(string clientName, DebugUtils.Debugger.Listeners.WCF.EventType mask);
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
internal interface IDebugServiceCallback {

    [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IDebugService/RequestApplicationInfo", ReplyAction = "http://tempuri.org/IDebugService/RequestApplicationInfoResponse")]
    DebugUtils.Debugger.Listeners.WCF.AppInfo RequestApplicationInfo();

    [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IDebugService/ExitApplication", ReplyAction = "http://tempuri.org/IDebugService/ExitApplicationResponse")]
    void ExitApplication();
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
internal interface IDebugServiceChannel : IDebugService, System.ServiceModel.IClientChannel {
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
internal partial class DebugServiceClient : System.ServiceModel.DuplexClientBase<IDebugService>, IDebugService {

    public DebugServiceClient(System.ServiceModel.InstanceContext callbackInstance) :
        base(callbackInstance) {
    }

    public DebugServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName) :
        base(callbackInstance, endpointConfigurationName) {
    }

    public DebugServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) :
        base(callbackInstance, endpointConfigurationName, remoteAddress) {
    }

    public DebugServiceClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
        base(callbackInstance, endpointConfigurationName, remoteAddress) {
    }

    public DebugServiceClient(System.ServiceModel.InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
        base(callbackInstance, binding, remoteAddress) {
    }

    public DebugUtils.Debugger.Listeners.WCF.ClientResponse Open(string clientName, string machineName, string processName) {
        return base.Channel.Open(clientName, machineName, processName);
    }

    public DebugUtils.Debugger.Listeners.WCF.ClientResponse HandleDebugMessage(string clientName, DebugUtils.Debugger.DebugMessage message) {
        return base.Channel.HandleDebugMessage(clientName, message);
    }

    public void Close(string clientName) {
        base.Channel.Close(clientName);
    }

    public void Subscribe(string clientName, DebugUtils.Debugger.Listeners.WCF.EventType mask) {
        base.Channel.Subscribe(clientName, mask);
    }

    public void Unsubscribe(string clientName, DebugUtils.Debugger.Listeners.WCF.EventType mask) {
        base.Channel.Unsubscribe(clientName, mask);
    }
}
