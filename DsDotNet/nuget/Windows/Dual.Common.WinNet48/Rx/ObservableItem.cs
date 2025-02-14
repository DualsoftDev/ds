using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsu.Common.Utilities
{
    /// <summary>
    /// OnNext 호출시, void type 으로 처리 결과를 반환받을 수 없으으로, 
    /// Data 를 wrapping 해서 처리 결과를 보고자 할 때에 사용한다.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <typeparam name="R"></typeparam>
    public class ObservableItem<TItem, R>
    {
        public TItem Item { get; private set; }
        public R OnNextReturnValue { get; set; }
        public ObservableItem(TItem item)
        {
            Item = item;
            //OnNextReturnValue = (default)R;
        }
    }
}
