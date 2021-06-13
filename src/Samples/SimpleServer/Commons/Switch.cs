using System;
using System.Threading.Tasks;

namespace Devices
{
    public enum ResultCode
    {
        Success, Failed
    }

    public class SetStatusResult
    {
        ResultCode _code = ResultCode.Success;
        bool _newStatus, _oldStatus;

        public SetStatusResult() : this(false) { }
        public SetStatusResult(bool oldStatus, bool newStatus) : this(ResultCode.Success, oldStatus, newStatus) { }
        public SetStatusResult(bool oldStatus) : this(ResultCode.Success, oldStatus, oldStatus) { }
        public SetStatusResult(ResultCode code, bool oldStatus, bool newStatus)
        {
            _code = code;
            _oldStatus = oldStatus;
            _newStatus = newStatus;
        }

        public ResultCode Code { get => _code; set => _code = value; }
        public bool OldStatus { get => _oldStatus; set => _oldStatus = value; }
        public bool NewStatus { get => _newStatus; set => _newStatus = value; }
    }

    public interface ISwitchApi
    {
        event EventHandler<bool> StatusChanged;
        ValueTask<SetStatusResult> TrySetStatusAsync(bool status);
        ValueTask<bool> GetStatusAsync();
        ValueTask<SetStatusResult> ToogleAsync();
        ValueTask CompleteAsync();
    }

    public class Switch : ISwitchApi
    {
        public bool _status;
        readonly TaskCompletionSource<bool> _completion;

        public event EventHandler<bool> StatusChanged;

        public Switch() : this(false)
        {
        }
        public Switch(bool initialStatus)
        {
            _status = initialStatus;
            _completion = new TaskCompletionSource<bool>();
        }
        public Task Completion => _completion.Task;

        public ValueTask<bool> GetStatusAsync() => new ValueTask<bool>(_status);

        public ValueTask<SetStatusResult> TrySetStatusAsync(bool status)
        {
            if (_status != status)
            {
                _status = status;
                StatusChanged?.Invoke(this, _status);
                return new ValueTask<SetStatusResult>(new SetStatusResult(!_status, _status));
            }
            return new ValueTask<SetStatusResult>(new SetStatusResult(_status));
        }

        public ValueTask<SetStatusResult> ToogleAsync() => TrySetStatusAsync(!_status);

        public ValueTask CompleteAsync()
        {
            _completion.SetResult(_status);
            return new ValueTask();
        }
    }


}
