using Soft.Generator.DesktopApp.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soft.Generator.DesktopApp.Controls
{
    public partial class SoftReturnButton : UserControl
    {
        PageNavigator _pageNavigator;

        public SoftReturnButton(PageNavigator pageNavigator)
        {
            _pageNavigator = pageNavigator;

            InitializeComponent();
        }

        private void btn_Return_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateBack();
        }
    }
}
