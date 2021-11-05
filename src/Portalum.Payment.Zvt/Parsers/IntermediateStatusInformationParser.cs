using Microsoft.Extensions.Logging;
using Portalum.Payment.Zvt.Repositories;
using System;

namespace Portalum.Payment.Zvt.Parsers
{
    /// <summary>
    /// IntermediateStatusInformationParser
    /// </summary>
    public class IntermediateStatusInformationParser : IIntermediateStatusInformationParser
    {
        private readonly ILogger _logger;
        private readonly IIntermediateStatusRepository _intermediateStatusRepository;

        /// <summary>
        /// IntermediateStatusInformationParser
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="intermediateStatusRepository"></param>
        public IntermediateStatusInformationParser(
            ILogger logger,
            IIntermediateStatusRepository intermediateStatusRepository)
        {
            this._logger = logger;
            this._intermediateStatusRepository = intermediateStatusRepository;
        }

        /// <inheritdoc />
        public string GetMessage(Span<byte> data)
        {
            if (data.Length <= 3)
            {
                this._logger.LogError($"{nameof(GetMessage)} - Invalid data length");
                return null;
            }

            var id = data[3];
            var message = this._intermediateStatusRepository.GetMessage(id);

            if (string.IsNullOrEmpty(message))
            {
                this._logger.LogError($"{nameof(GetMessage)} - No message available for {id:X2}");
            }

            return message;
        }
    }
}
