using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Services
{
    public class PageNavigator
    {
        private Stack<UserControl> _pageStack = new Stack<UserControl>();
        private Panel _pnl_Main;

        public PageNavigator() { }

        public void InitializeMainPanel(Panel pnl_Main)
        {
            _pnl_Main = pnl_Main;
        }

        /// <summary>
        /// Pass <paramref name="currentPage"/> if you want to have the return button on the page which you are navigating to.
        /// </summary>
        public void NavigateToPage(UserControl page, UserControl currentPage = null)
        {
            _pnl_Main.Controls.Clear();

            page.Dock = DockStyle.Fill;

            _pnl_Main.Controls.Add(page);

            if (currentPage != null && (_pageStack.Count == 0 || _pageStack.Peek() != currentPage))
            {
                _pageStack.Push(currentPage);
            }
        }

        public void NavigateBack()
        {
            if (_pageStack.Count > 0)
            {
                UserControl previousPage = _pageStack.Pop();

                _pnl_Main.Controls.Clear();

                previousPage.Dock = DockStyle.Fill;

                _pnl_Main.Controls.Add(previousPage);
            }
        }
    }
}
