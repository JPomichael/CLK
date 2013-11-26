﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace CLK.ServiceModel
{
    public abstract class ConnectionServiceHost<TConnectionService> : ConnectionHost<TConnectionService>
        where TConnectionService : ConnectionService
    {
        // Fields   
        private readonly ServiceHost _serviceHost = null;

        private readonly ConnectionServiceMediator _serviceMediator = null;
               

        // Constructors
        public ConnectionServiceHost(ServiceHost serviceHost)
        {
            #region Contracts

            if (serviceHost == null) throw new ArgumentNullException();

            #endregion

            // ServiceHost
            _serviceHost = serviceHost;

            // ServiceMediator
            _serviceMediator = new ConnectionServiceMediator();
            _serviceMediator.Connected += this.ConnectionService_Connected;
            _serviceMediator.Disconnected += this.ConnectionService_Disconnected;
        }
                        

        // Methods
        public virtual void Open()
        {
            // AttachResource
            this.AttachResource(this);

            // ServiceHost
            _serviceHost.Open();
        }

        public virtual void Close()
        {
            // ServiceHost
            _serviceHost.Abort();

            // DetachResource
            this.DetachResource(this);
        }
        

        protected void AttachResource(object resource)
        {
            #region Contracts

            if (resource == null) throw new ArgumentNullException();

            #endregion

            // Resource
            ConnectionServiceResource.Current.AttachResource(_serviceHost, resource);
        }

        protected void DetachResource(object resource)
        {
            #region Contracts

            if (resource == null) throw new ArgumentNullException();

            #endregion
                        
            // Resource
            ConnectionServiceResource.Current.DetachResource(_serviceHost, resource);
        }


        // Handlers
        private void ConnectionService_Disconnected(object sender, EventArgs e)
        {
            #region Contracts

            if (sender == null) throw new ArgumentException();
            if (e == null) throw new ArgumentException();

            #endregion

            // Require
            TConnectionService connectionService = sender as TConnectionService;
            if (connectionService == null) return;

            // Attach
            this.Attach(connectionService);
        }

        private void ConnectionService_Connected(object sender, EventArgs e)
        {
            #region Contracts

            if (sender == null) throw new ArgumentException();
            if (e == null) throw new ArgumentException();

            #endregion

            // Require
            TConnectionService connectionService = sender as TConnectionService;
            if (connectionService == null) return;

            // Detach
            this.Detach(connectionService);
        }
    }
}