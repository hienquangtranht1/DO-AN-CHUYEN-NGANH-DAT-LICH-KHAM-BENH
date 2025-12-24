using BookinhMVC.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BookinhMVC.Services
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}