using System.Threading;
using System.Threading.Tasks;
using Core.Bank;
using Core.Commands;
using Core.Mappers;
using Core.Responses;
using MediatR;
using SharedKernel.Interfaces;

namespace Core.Handlers
{
    public class PaymentProcessHandler : IRequestHandler<PaymentProcessCommand, PaymentProcessResponse>
    {
        private readonly IAcquiringBank bank;
        private readonly IRepository repository;

        public PaymentProcessHandler(IAcquiringBank bank, IRepository repository)
        {
            this.bank = bank;
            this.repository = repository;
        }

        public async Task<PaymentProcessResponse> Handle(PaymentProcessCommand request,
            CancellationToken cancellationToken)
        {
            var payment = new PaymentMapper().Map(request);

            var response = await bank.ProcessPayment(payment);

            payment.TransactionId = response.TransactionId;
            payment.TransactionStatus = response.Status;

            if (response is PaymentProcessErrorResponse errorResponse)
            {
                payment.Reason = errorResponse.Reason;
                // TODO - Logger error message with an Event
            }

            var newPayment = await repository.AddAsync(payment);

            return new PaymentProcessResponseMapper().Map(newPayment);
        }
    }
}