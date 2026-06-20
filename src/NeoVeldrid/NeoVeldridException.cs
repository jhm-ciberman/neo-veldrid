using System;

namespace NeoVeldrid
{
    /// <summary>
    /// Represents errors that occur in the NeoVeldrid library.
    /// </summary>
    public class NeoVeldridException : Exception
    {
        /// <summary>
        /// Constructs a new NeoVeldridException.
        /// </summary>
        public NeoVeldridException()
        {
        }

        /// <summary>
        /// Constructs a new NeoVeldridexception with the given message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public NeoVeldridException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructs a new NeoVeldridexception with the given message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public NeoVeldridException(string message, Exception innerException) : base(message, innerException)
        {
        }

        internal static string Describe(BindableResource resource)
        {
            // A DeviceBufferRange is the one bindable that isn't a DeviceResource; describe it by its buffer.
            return resource is DeviceBufferRange range
                ? Describe((DeviceResource)range.Buffer)
                : Describe(resource as DeviceResource);
        }

        internal static string Describe(DeviceResource resource)
        {
            string kind = resource switch
            {
                Texture => nameof(Texture),
                TextureView => nameof(TextureView),
                DeviceBuffer => nameof(DeviceBuffer),
                Sampler => nameof(Sampler),
                _ => "resource",
            };
            string name = resource?.Name;
            return string.IsNullOrEmpty(name) ? kind : $"{kind} '{name}'";
        }
    }
}
