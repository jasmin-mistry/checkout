using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Commands;
using Core.Queries;
using Core.Responses;
using FluentValidation.Results;
using Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PaymentGateway.Controllers
{
    public class PaymentsController : BaseApiController
    {
        private readonly ILogger<PaymentsController> logger;
        private readonly IMediator mediator;

        public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger = null)
        {
            this.mediator = mediator;
            this.logger = logger ?? new ConsoleLogger<PaymentsController>();
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaymentResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(EmptyResult))]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetPaymentByIdQuery(id);
            var result = await mediator.Send(query);

            using (logger.BeginScope(new Dictionary<string, object>
            {
                {"paymentId", id}
            }))
            {
                if (result != null)
                {
                    logger.LogInformation("Payment fetched.");
                    return Ok(result);
                }

                logger.LogInformation("Payment was not found");

                return NotFound();
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaymentProcessResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IEnumerable<ValidationFailure>))]
        public async Task<IActionResult> Post([FromBody] PaymentProcessCommand command)
        {
            var result = await mediator.Send(command);
            using (logger.BeginScope(new Dictionary<string, object>
            {
                {"paymentId", result.PaymentId}
            }))
            {
                logger.LogInformation("Payment processed");
                return Ok(result);
            }
        }
    }
}