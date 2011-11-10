using System;
using System.Linq;

namespace Gate.Helpers
{
    public enum BodyStreamCommand
    {
        Start,
        Pause,
        Stop,
        Cancel,
        Resume
    }

    public enum BodyStreamState
    {
        Ready,
        Started,
        Paused,
        Stopped,
        Cancelled,
        Resumed
    }

    public class BodyStream : StateMachine<BodyStreamCommand, BodyStreamState>
    {
        public Func<ArraySegment<byte>, Action, bool> Next { get; private set; }
        public Action<Exception> Error { get; private set; }
        public Action Complete { get; private set; }

        public BodyStream(Func<ArraySegment<byte>, Action, bool> data, Action<Exception> error, Action complete)
        {
            Initialize(BodyStreamState.Ready);

            MapTransition(BodyStreamCommand.Pause, BodyStreamState.Paused);
            MapTransition(BodyStreamCommand.Start, BodyStreamState.Started);
            MapTransition(BodyStreamCommand.Cancel, BodyStreamState.Cancelled);
            MapTransition(BodyStreamCommand.Resume, BodyStreamState.Resumed);
            MapTransition(BodyStreamCommand.Stop, BodyStreamState.Stopped);

            Next = data;
            Error = error;
            Complete = complete;
        }

        public void Start(Action start, Action dispose)
        {
            if (start == null)
            {
                throw new ArgumentNullException("start", "Missing start action for the BodyStream.");
            }

            On(BodyStreamCommand.Start, start);
            Invoke(BodyStreamCommand.Start);

            if (dispose != null)
            {
                foreach (var command in new[] { BodyStreamCommand.Stop, BodyStreamCommand.Cancel })
                {
                    On(command, dispose);
                }
            }
        }

        public void Finish()
        {
            Stop();
            Complete();
        }

        public void SendBytes(ArraySegment<byte> part, Action continuation, Action complete)
        {
            Action resume = null;
            Action pause = () => { };

            if (continuation != null)
            {
                On(BodyStreamCommand.Resume, continuation);
                resume = Resume;
                pause = Pause;
            }

            // call on-next with back-pressure support
            if (Next(part, resume))
            {
                pause.Invoke();
            }

            if (complete != null)
            {
                complete.Invoke();
            }
        }

        public void Cancel()
        {
            Invoke(BodyStreamCommand.Cancel);
        }

        public void Pause()
        {
            Invoke(BodyStreamCommand.Pause);
        }

        public void Resume()
        {
            Invoke(BodyStreamCommand.Resume);
        }

        public void Stop()
        {
            Invoke(BodyStreamCommand.Stop);
        }

        public bool CanSend()
        {
            var validStates = new[] { BodyStreamState.Started, BodyStreamState.Resumed };
            return validStates.Contains(State);
        }
    }
}