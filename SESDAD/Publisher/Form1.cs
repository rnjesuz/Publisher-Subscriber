using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SESDAD
{
    public partial class Form1 : Form
    {
        private RemotePublisher RP =  new RemotePublisher();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void TopicButton_Click(object sender, EventArgs e)
        {
            RP.ChangeTopic(TopicTextBox.Text);
        }

        private void TopicTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void BrokerConnectionButton_Click(object sender, EventArgs e)
        {
            RP.ConnectBroker();
        }
    }
}
