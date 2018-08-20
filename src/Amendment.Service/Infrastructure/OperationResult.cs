using System;
using System.Collections.Generic;
using System.Text;

namespace Amendment.Service.Infrastructure
{
    public interface IOperationResult
    {
        bool Succeeded { get; }
        List<string> Errors { get; }
    }

    public class OperationResult : IOperationResult
    {
        public bool Succeeded { get; set; }
        public List<string> Errors { get; }

        public OperationResult()
        {
            Succeeded = true;
            Errors = new List<string>();
        }

        public OperationResult(bool succeeded)
        {
            Succeeded = succeeded;
            Errors = new List<string>();
        }

        public OperationResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Errors = new List<string>() { message };
        }

        public void AddResult(string result)
        {
            if (result != null)
                Errors.Add(result);
        }
    }
}
