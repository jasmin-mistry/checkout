using System;
using Core.Responses;
using MediatR;

namespace Core.Queries
{
    public class GetPaymentByIdQuery : IRequest<PaymentResponse>
    {
        public GetPaymentByIdQuery(Guid id)
        {
            PaymentId = id;
        }

        public Guid PaymentId { get; }
    }
}