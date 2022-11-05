using System;

using ZXMAK2.Dependency;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Presentation.Interfaces;


namespace ZXMAK2.Hardware.Sprinter
{
    public class SprinterDebugger : BusDeviceBase, IJtagDevice
    {
        private ViewHolder<IDebuggerSprinterView> m_viewHolder;

        public SprinterDebugger()
        {
            Category = BusDeviceCategory.Debugger;
            Name = "DEBUGGER SPRINTER";
            Description = "Extended Debugger for Sprinter";
            CreateViewHolder();
        }


        #region IJtagDevice

        public void Attach(IDebuggable dbg)
        {
            if (m_viewHolder != null && dbg != null)
                m_viewHolder.SetupView = d => d.Init(dbg);
        }

        public void Detach()
        {
            if (m_viewHolder != null)
            {
                m_viewHolder.Close();
                m_viewHolder.SetupView = null;
            }
        }

        #endregion

        
        #region IBusDevice

        public override void BusInit(IBusManager bmgr)
        {
            if (m_viewHolder != null)
            {
                bmgr.AddCommandUi(m_viewHolder.CommandOpen);
            }
        }

        public override void BusConnect()
        {
            
        }

        public override void BusDisconnect()
        {
            if (m_viewHolder != null)
            {
                m_viewHolder.Close();
            }
        }

        #endregion


        #region IGuiExtension

        private void CreateViewHolder()
        {
            try
            {
                m_viewHolder = new ViewHolder<IDebuggerSprinterView>("Debugger", _ => { });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        #endregion IGuiExtension
    }
}
