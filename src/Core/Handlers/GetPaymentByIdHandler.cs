using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Mappers;
using Core.Queries;
using Core.Responses;
using MediatR;
using SharedKernel.Interfaces;

namespace Core.Handlers
{
    public class GetPaymentByIdHandler : IRequestHandler<GetPaymentByIdQuery, PaymentResponse>
    {
        private readonly IRepository repository;

        public GetPaymentByIdHandler(IRepository repository)
        {
            this.repository = repository;
        }

        public async Task<PaymentResponse> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
        {
            var payment = await repository.GetByIdAsync<Payment>(request.PaymentId);
            return new PaymentResponseMapper().Map(payment);
        }
    }
}