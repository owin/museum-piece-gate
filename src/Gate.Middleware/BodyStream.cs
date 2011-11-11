using System;
using System.Linq;
using Gate.Helpers;

namespace Gate.Middleware
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

    public class BodyStream
    {
        private readonly StateMachine<BodyStreamCommand, BodyStreamState> stateMachine;

        public Func<ArraySegment<byte>, Action, bool> Next { get; private set; }
        public Action<Exception> Error { get; private set; }
        public Action Complete { get; private set; }

        public BodyStream(Func<ArraySegment<byte>, Action, bool> data, Action<Exception> error, Action complete)
        {
            stateMachine = new StateMachine<BodyStreamCommand, BodyStreamState>();
            stateMachine.Initialize(BodyStreamState.Ready);

            stateMachine.MapTransition(BodyStreamCommand.Pause, BodyStreamState.Paused);
            stateMachine.MapTransition(BodyStreamCommand.Start, BodyStreamState.Started);
            stateMachine.MapTransition(BodyStreamCommand.Cancel, BodyStreamState.Cancelled);
            stateMachine.MapTransition(BodyStreamCommand.Resume, BodyStreamState.Resumed);
            stateMachine.MapTransition(BodyStreamCommand.Stop, BodyStreamState.Stopped);

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

            stateMachine.On(BodyStreamCommand.Start, start);
            stateMachine.Invoke(BodyStreamCommand.Start);

            if (dispose != null)
            {
                foreach (var command in new[] { BodyStreamCommand.Stop, BodyStreamCommand.Cancel })
                {
                    stateMachine.On(command, dispose);
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
                stateMachine.On(BodyStreamCommand.Resume, continuation);
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
            stateMachine.Invoke(BodyStreamCommand.Cancel);
        }

        public void Pause()
        {
            stateMachine.Invoke(BodyStreamCommand.Pause);
        }

        public void Resume()
        {
            stateMachine.Invoke(BodyStreamCommand.Resume);
        }

        public void Stop()
        {
            stateMachine.Invoke(BodyStreamCommand.Stop);
        }

        public bool CanSend()
        {
            var validStates = new[] { BodyStreamState.Started, BodyStreamState.Resumed };
            return validStates.Contains(stateMachine.State);
        }
    }
}