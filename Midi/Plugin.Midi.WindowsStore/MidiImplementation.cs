using Plugin.Midi.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Midi
{
    /// <summary>
    /// Implementation for Midi
    /// </summary>
    public class MidiImplementation : IMidi
    {
        public event EventHandler<MidiDeviceConnectionEventArgs> DeviceAdded;
        public event EventHandler<MidiDeviceConnectionEventArgs> DeviceRemoved;

        public IEnumerable<MidiDeviceDetails> GetDevices()
        {
            throw new NotImplementedException();
        }

        public Task<IMidiDevice> OpenDevice(string id)
        {
            throw new NotImplementedException();
        }
    }
}