using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;

namespace Business
{
    public class OrderManage
    {
        public static List<OrderInfo> GetOrders(int count)
        {
            List<OrderInfo> orders = new List<OrderInfo>();
            orders.Add(new OrderInfo()
            {
                OrderId = 1,
                MobileNum = "18281710971",
                Price = 10
            });
            orders.Add(new OrderInfo()
            {
                OrderId = 2,
                MobileNum = "18281710971",
                Price = 20
            });

            return orders;
        }
    }
}
