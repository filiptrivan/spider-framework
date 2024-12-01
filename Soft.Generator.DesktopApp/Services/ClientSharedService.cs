using Soft.Generator.DesktopApp.Controllers;
using Soft.Generator.DesktopApp.Controls;
using Soft.Generator.DesktopApp.Interfaces;
using Soft.Generator.DesktopApp.Pages.CompanyPages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Services
{
    public class ClientSharedService
    {

        public ClientSharedService()
        {
        }

        public void ShowSuccessfullMessage()
        {
            MessageBox.Show("Uspešno ste izvršili akciju.", "Uspešna akcija");
        }

        public void CellContentClickHandler<TDetailsPage, TDetailsEntity, TDetailsEntityID>(
            DataGridViewCellEventArgs e, 
            UserControl currentPage,
            SoftDataGridView softDataGridView,
            Func<UserControl, TDetailsPage> NavigateToDetailsPage,
            Func<TDetailsEntityID, TDetailsEntity> GetEntity,
            Action<TDetailsEntityID> DeleteEntity,
            Action LoadTable
        )
            where TDetailsPage : UserControl, ISoftDetailsPage
            where TDetailsEntity : class, ISoftEntity
            where TDetailsEntityID : struct
        {
            DataGridViewColumn detailsColumn = softDataGridView.ColumnCollection["Details"];
            TDetailsEntityID id = (TDetailsEntityID)softDataGridView.RowCollection[e.RowIndex].Cells["Id"].Value;

            if (detailsColumn != null && e.ColumnIndex == detailsColumn.Index)
            {
                TDetailsPage detailsPage = NavigateToDetailsPage(currentPage);
                detailsPage.Initialize(GetEntity(id));
            }

            DataGridViewColumn deleteColumn = softDataGridView.ColumnCollection["Delete"];

            if (deleteColumn != null && e.ColumnIndex == deleteColumn.Index)
            {
                DialogResult dialogResult = MessageBox.Show("Da li ste sigurni da želite da obrišete objekat?", "Potvrda brisanja", MessageBoxButtons.YesNoCancel);

                if (dialogResult == DialogResult.Yes)
                {
                    DeleteEntity(id);
                    LoadTable();

                    ShowSuccessfullMessage();
                }
            }
        }
    }
}
