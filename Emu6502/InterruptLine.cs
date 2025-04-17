using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emu6502
{
    public class InterruptLine
    {
        public readonly bool edgeTriggered;

        /// <summary>
        /// True if something is holding the interrupt line.
        /// </summary>
        public bool Triggered => interruptors.Count > 0;
        /// <summary>
        /// Checks to see if the line should trigger an interrupt. If
        /// edgeTriggered, then this will only return true once until the line
        /// goes low and then high again.
        /// </summary>
        public bool ShouldInterrupt
        {
            get
            {
                if (!Triggered)
                    return false;

                if (!edgeTriggered)
                {
                    hasTriggeredSinceLastEdge = true;
                    return true;
                }

                if (hasTriggeredSinceLastEdge)
                    return false;
                hasTriggeredSinceLastEdge = true;
                return true;
            }
        }

        private readonly HashSet<object> interruptors;
        /// <summary>
        /// If true, that means that the interrupt has been acknowledged since the last not-interrupting-to-interrupting edge.
        /// </summary>
        private bool hasTriggeredSinceLastEdge;

        /// <param name="edgeTriggered">Makes this interrupt line edge triggered, else its level triggered.</param>
        public InterruptLine(bool edgeTriggered)
        {
            this.edgeTriggered = edgeTriggered;
            interruptors = new HashSet<object>();
            hasTriggeredSinceLastEdge = false;
        }

        public bool TriggerInterrupt(object interruptor)
        {
            if (!Triggered)
                hasTriggeredSinceLastEdge = false;
            return interruptors.Add(interruptor);
        }

        public bool IsTriggering(object interruptor)
        {
            return interruptors.Contains(interruptor);
        }

        public bool ClearInterrupt(object interruptor)
        {
            return interruptors.Remove(interruptor);
        }
    }
}
