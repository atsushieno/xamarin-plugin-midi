using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Midi.Abstractions
{
	/// <summary>
	/// Interface for Midi
	/// </summary>
	public interface IMidi
	{
		IEnumerable<MidiDeviceDetails> GetDevices ();
		Task<IMidiDevice> OpenDevice (string id);
		event EventHandler<MidiDeviceConnectionEventArgs> DeviceAdded;
		event EventHandler<MidiDeviceConnectionEventArgs> DeviceRemoved;
	}

	public class MidiDeviceConnectionEventArgs : EventArgs
	{
		public MidiDeviceDetails Device { get; set; }
	}

	public enum MidiPortType
	{
		Input,
		Output,
	}

	public class MidiDeviceDetails
	{
		public MidiDeviceDetails (string id, string name, IEnumerable<MidiPortDetails> ports)
		{
			this.Id = id;
			this.Name = name;
			Ports = ports.ToList ();
		}

		public string Id { get; set; }
		public string Name { get; set; }

		public IList<MidiPortDetails> Ports { get; private set; }
	}

	public class MidiPortDetails
	{
		public MidiPortDetails (MidiPortType portType, string name, string portId)
		{
			this.PortType = portType;
			this.Name = name;
			this.PortId = portId;
		}

		public MidiPortType PortType { get; set; }
		public string Name { get; set; }
		public string PortId { get; set; }
	}

	public interface IMidiDevice : IDisposable
	{
		MidiDeviceDetails Details { get; }
		Task<IMidiInputPort> OpenInput (string portId);
		Task<IMidiOutputPort> OpenOutput (string portId);
	}

	public interface IMidiPort : IDisposable
	{
		MidiPortDetails Details { get; }

		void Close ();
	}

	public interface IMidiInputPort : IMidiPort
	{
		event EventHandler<MidiMessageEventArgs> MessageReceived;
	}

	public class MidiMessageEventArgs : EventArgs
	{
		public MidiMessageEventArgs (IEnumerable<MidiMessage> messages)
		{
			Messages = messages.ToList ();
		}

		public IList<MidiMessage> Messages { get; private set; }
	}

	public class MidiMessage
	{
		public MidiMessage (ArraySegment<byte> message, long timestamp)
		{
			Message = message;
			Timestamp = timestamp;
		}

		public long Timestamp { get; private set; }
		public ArraySegment<byte> Message { get; set; }
	}

	public interface IMidiOutputPort : IMidiPort
	{
		void Send (byte [] msg, int offset, int length);
		void Send (byte [] msg, int offset, int length, long timestamp);
	}
}
