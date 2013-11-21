﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CLK.ServiceModel
{
    public abstract class ConnectionProxyHostBase<TConnectionProxy, TConnection> : ConnectionHost<TConnection>
        where TConnectionProxy : ConnectionProxy
        where TConnection : class
    {
        // Fields
        private readonly object _syncObject = new object();

        private readonly IEnumerable<TConnection> _connectionCollection = null;

        private readonly IEnumerable<TConnectionProxy> _connectionProxyCollection = null;

        private readonly Func<IEnumerable<ConnectionProxy>, bool> _connectedPredicate = null;

        private bool _isConnected = false;


        // Constructors        
        internal ConnectionProxyHostBase(IEnumerable<TConnectionProxy> connectionProxyCollection, Func<IEnumerable<ConnectionProxy>, bool> connectedPredicate)
        {
            #region Contracts

            if (connectionProxyCollection == null) throw new ArgumentNullException();
            if (connectedPredicate == null) throw new ArgumentNullException();

            #endregion
                        
            // ConnectionProxyCollection
            _connectionProxyCollection = connectionProxyCollection;

            // ConnectedPredicate 
            _connectedPredicate = connectedPredicate;

            // ConnectionCollection
            List<TConnection> connectionCollection = new List<TConnection>();
            foreach (TConnectionProxy connectionProxy in connectionProxyCollection)
            {
                TConnection connection = this.CreateConnection(connectionProxy);
                if (connection == null) throw new InvalidOperationException();
                connectionCollection.Add(connection);
            }
            _connectionCollection = connectionCollection;
        }


        // Properties
        public bool IsConnected
        {
            get
            {
                lock (_syncObject)
                {
                    return _isConnected;
                }
            }
        }


        // Methods
        public void Open()
        {
            // ConnectionCollection
            foreach (TConnection connection in _connectionCollection)
            {
                this.Attach(connection);
            }

            // ConnectionProxyCollection
            foreach (TConnectionProxy connectionProxy in _connectionProxyCollection)
            {
                connectionProxy.Connected += this.ConnectionProxy_Connected;
                connectionProxy.Disconnected += this.ConnectionProxy_Disconnected;
                connectionProxy.Heartbeating += this.ConnectionProxy_Heartbeating;
                connectionProxy.Open();
            }
        }

        public void Close()
        {
            // ConnectionProxyCollection
            foreach (TConnectionProxy connectionProxy in _connectionProxyCollection)
            {
                connectionProxy.Close();
                connectionProxy.Connected -= this.ConnectionProxy_Connected;
                connectionProxy.Disconnected -= this.ConnectionProxy_Disconnected;
                connectionProxy.Heartbeating -= this.ConnectionProxy_Heartbeating;
            }

            // ConnectionCollection
            foreach (TConnection connection in _connectionCollection)
            {
                this.Detach(connection);
            }
        }

        private void Refresh()
        {
            // IsConnected
            bool isConnected = _connectedPredicate(_connectionProxyCollection);

            // Require
            lock (_syncObject)
            {
                if (_isConnected == isConnected) return;
                _isConnected = isConnected;
            }

            // Notify
            if (isConnected == true)
            {
                this.OnConnected();
            }
            else
            {
                this.OnDisconnected();
            }
        }

        internal abstract TConnection CreateConnection(TConnectionProxy connectionProxy);


        // Handlers
        private void ConnectionProxy_Connected(object sender, EventArgs e)
        {
            // Refresh
            this.Refresh();
        }

        private void ConnectionProxy_Disconnected(object sender, EventArgs e)
        {
            // Refresh
            this.Refresh();
        }

        private void ConnectionProxy_Heartbeating(object sender, EventArgs e)
        {
            #region Contracts

            if (sender == null) throw new ArgumentException();
            if (e == null) throw new ArgumentException();

            #endregion

            // Require
            if (this.IsConnected == false) return;

            // Notify
            this.OnHeartbeating();
        }


        // Events
        public event EventHandler Connected;
        private void OnConnected()
        {
            var handler = this.Connected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public event EventHandler Disconnected;
        private void OnDisconnected()
        {
            var handler = this.Disconnected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public event EventHandler Heartbeating;
        private void OnHeartbeating()
        {
            var handler = this.Heartbeating;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}