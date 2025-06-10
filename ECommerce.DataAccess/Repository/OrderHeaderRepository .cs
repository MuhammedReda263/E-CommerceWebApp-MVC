using ECommerce.DataAccess.Data;
using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _DbContext;

        public OrderHeaderRepository(ApplicationDbContext DbContext) : base(DbContext) 
        {
            _DbContext = DbContext;
        }
        public void Update(OrderHeader obj)
        {
            _DbContext.OrderHeaders.Update(obj);
        }

        public void UpdateStatus(int id, string OrderStatus, string? PaymentStatus = null)
        {
            var OrderHeader = _DbContext.OrderHeaders.FirstOrDefault(x => x.Id == id);
            if (OrderHeader != null)
            {
                OrderHeader.OrderStatus = OrderStatus;
                if (!string.IsNullOrEmpty(PaymentStatus))
                {
                    OrderHeader.PaymentStatus = PaymentStatus;
                }
            }
        }

        public void UpdateStripPaymentId(int id, string sessionId, string PaymentIntedId)
        {
            var OrderHeader = _DbContext.OrderHeaders.FirstOrDefault(x => x.Id == id);
            if (OrderHeader != null)
            {
                if (!string.IsNullOrEmpty(sessionId))
                {
                    OrderHeader.SessionId = sessionId;
                }
                if (!string.IsNullOrEmpty(PaymentIntedId))
                {
                    OrderHeader.PaymentIntentId = PaymentIntedId;
                    OrderHeader.PaymentDate = DateTime.Now;
                }
                   
            }
        }
        }
    }
