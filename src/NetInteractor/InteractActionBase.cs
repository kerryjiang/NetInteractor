using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using NetInteractor.Config;

namespace NetInteractor
{
    public abstract class InteractionBase<TConfig> : IInteractAction
        where TConfig : class
    {
        public TConfig Config { get; }

        public InteractionBase(TConfig config)
        {
            Config = config;
        }

        public abstract Task<InteractionResult> ExecuteAsync(InterationContext context);

        protected string PrepareValue(InterationContext context, string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            while (true)
            {
                var pos = value.IndexOf("${");
                var endPos = -1;

                if (pos >= 0)
                {
                    endPos = value.IndexOf("}", pos);

                    if (endPos >= 0)
                    {                     
                        var outputPropertyName = value.Substring(pos + 2, endPos - pos - 2);
                        value = value.Substring(0, pos) + GetOutputValue(context.Outputs, outputPropertyName) + value.Substring(endPos + 1);
                        continue;
                    }
                }

                pos = value.IndexOf("$(");

                if (pos >= 0)
                {
                    endPos = value.IndexOf(")", pos);

                    if (endPos >= 0)
                    {                     
                        var inputPropertyName = value.Substring(pos + 2, endPos - pos - 2);
                        value = value.Substring(0, pos) + GetOutputValue(context.Inputs, inputPropertyName) + value.Substring(endPos + 1);
                        continue;
                    }
                }

                break;
            }

            return value;       
        }

        protected string GetInputValue(NameValueCollection inputValues, string property)
        {
            if (inputValues == null || inputValues.Count <= 0)
                return string.Empty;

            return inputValues[property];
        }

        protected string GetOutputValue(NameValueCollection outputValues, string property)
        {
            if (outputValues == null || outputValues.Count <= 0)
                return string.Empty;

            return outputValues[property];
        }

        protected string GetValue(InterationContext context, string property)
        {
            if (property.StartsWith("$(") && property.EndsWith(")"))
            {
                var inputPropertyName = property.Substring(1, property.Length - 3);
                return GetInputValue(context.Inputs, inputPropertyName);
            }

            if (property.StartsWith("${") && property.EndsWith("}"))
            {
                var outputPropertyName = property.Substring(1, property.Length - 3);
                return GetOutputValue(context.Outputs, outputPropertyName);
            }

            return string.Empty;
        }
    }
}