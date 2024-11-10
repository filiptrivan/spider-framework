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

namespace Soft.Generator.DesktopApp.Pages.FrameworkPages
{
    public partial class FrameworkDetailsPage : UserControl
    {
        PageNavigator _pageNavigator;

        public FrameworkDetailsPage(PageNavigator pageNavigator)
        {
            _pageNavigator = pageNavigator;

            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _pageNavigator.NavigateBack();
        }
    }
}
