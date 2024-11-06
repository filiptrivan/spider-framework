using Microsoft.Data.SqlClient;
using Soft.Generator.DesktopApp.Entities;
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

namespace Soft.Generator.DesktopApp.Pages
{
    public partial class PermissionPage : UserControl
    {
        private readonly SqlConnection _connection;
        private readonly DesktopAppBusinessService _desktopAppService;

        public PermissionPage(SqlConnection connection, DesktopAppBusinessService desktopAppService)
        {
            _connection = connection;
            _desktopAppService = desktopAppService;

            InitializeComponent();

            dataGridView1.DataSource = new BindingList<Permission>(_desktopAppService.GetPermissionList());
        }


    }
}
