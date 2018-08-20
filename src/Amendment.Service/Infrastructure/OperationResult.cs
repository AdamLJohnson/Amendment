using System;
using System.Collections.Generic;
using System.Text;

namespace Amendment.Service.Infrastructure
{
    public interface IOperationResult
    {
        bool Succeeded { get; }
        List<string> Errors { get; }
        OperationType OperationType { get; }
    }

    public class OperationResult : IOperationResult
    {
        public bool Succeeded { get; set; }
        public List<string> Errors { get; }
        public OperationType OperationType { get; }

        public OperationResult(OperationType operationType)
        {
            OperationType = operationType;
            Succeeded = true;
            Errors = new List<string>();
        }

        public OperationResult(OperationType operationType, bool succeeded)
        {
            OperationType = operationType;
            Succeeded = succeeded;
            Errors = new List<string>();
        }

        public OperationResult(OperationType operationType, bool succeeded, string message)
        {
            OperationType = operationType;
            Succeeded = succeeded;
            Errors = new List<string>() { message };
        }

        public void AddResult(string result)
        {
            if (result != null)
                Errors.Add(result);
        }
    }

    public enum OperationType
    {
        Create = 1,
        Update = 2,
        Delete = 3
    }
}
