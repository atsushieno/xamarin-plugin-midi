using Plugin.Midi.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.Storage.Streams;

namespace Plugin.Midi
{
    /// <summary>
    /// Implementation for Midi
    /// </summary>
    public class MidiImplementation : IMidi
    {
        public event EventHandler<MidiDeviceConnectionEventArgs> DeviceAdded;
        public event EventHandler<MidiDeviceConnectionEventArgs> DeviceRemoved;

        FakeMidiDevice singleton;

        public IEnumerable<MidiDeviceDetails> GetDevices()
        {
            if (singleton == null)
                singleton = new FakeMidiDevice();
            yield return new MidiDeviceDetails("devices", "devices",
                singleton.InputPortInfos.Concat (singleton.OutputPortInfos));
        }

        public Task<IMidiDevice> OpenDevice(string id)
        {
            return Task.FromResult((IMidiDevice)new FakeMidiDevice());
            throw new NotImplementedException();
        }

        class FakeMidiDevice : IMidiDevice
        {
            public FakeMidiDevice()
            {
                int ports = 0;
                Inputs = DeviceInformation.FindAllAsync(MidiInPort.GetDeviceSelector()).GetResults();
                InputPortInfos = Inputs.Select(i => new WindowsPortInfo(i.Id, MidiPortType.Input, i.Name, ports++)).ToArray(); // freeze for port number
                Outputs = DeviceInformation.FindAllAsync(MidiOutPort.GetDeviceSelector()).GetResults();
                OutputPortInfos = Outputs.Select(i => new WindowsPortInfo(i.Id, MidiPortType.Input, i.Name, ports++)).ToArray(); // freeze for port number
            }

            internal class WindowsPortInfo : MidiPortInfo
            {
                public WindowsPortInfo (string id, MidiPortType portType, string name, int portNumber) : base (portType, name, portNumber)
                {
                    Id = id;
                }

                public string Id { get; set; }
            }

            public DeviceInformationCollection Inputs { get; private set; }
            public DeviceInformationCollection Outputs { get; private set; }
            internal IList<WindowsPortInfo> InputPortInfos { get; private set; }
            internal IList<WindowsPortInfo> OutputPortInfos { get; private set; }

            public async Task<IMidiInputPort> OpenInput(int portNumber)
            {
                var port = InputPortInfos.First(p => p.PortNumber == portNumber);
                var result = MidiInPort.FromIdAsync(port.Id);
                await result;
                return new WindowsMidiInputPort (result.GetResults());
            }

            public async Task<IMidiOutputPort> OpenOutput(int portNumber)
            {
                var port = OutputPortInfos.First(p => p.PortNumber == portNumber);
                var result = MidiOutPort.FromIdAsync(port.Id);
                await result;
                return new WindowsMidiOutputPort(result.GetResults());
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            private class WindowsMidiInputPort : IMidiInputPort
            {
                private MidiInPort port;

                public WindowsMidiInputPort(MidiInPort midiInPort)
                {
                    this.port = midiInPort;
                }

                public MidiDeviceDetails Details
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public event EventHandler<MidiMessageEventArgs> MessageReceived;

                public void Close()
                {
                    port.Dispose();
                }

                public void Dispose()
                {
                    Close();
                }
            }

            private class WindowsMidiOutputPort : IMidiOutputPort
            {
                private IMidiOutPort port;

                public WindowsMidiOutputPort(IMidiOutPort midiOutPort)
                {
                    this.port = midiOutPort;
                }

                public MidiDeviceDetails Details
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                public void Close()
                {
                    port.Dispose();
                }

                public void Dispose()
                {
                    Close();
                }

                public void Send(byte[] msg, int offset, int length)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}