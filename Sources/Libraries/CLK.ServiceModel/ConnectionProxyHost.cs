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
    public interface IConnectionProxyHost
    {
        // Properties
        bool IsConnected { get; }


        // Methods
        void Open();

        void Close();


        // Events
        event EventHandler Connected;

        event EventHandler Disconnected;

        event EventHandler Heartbeating;
    }    

    public abstract class ConnectionProxyHostBase<TConnectionProxy> : IConnectionProxyHost
        where TConnectionProxy : ConnectionProxy
    {
        // Fields
        private readonly object _syncObject = new object();

        private readonly IEnumerable<TConnectionProxy> _connectionProxyCollection = null;

        private readonly Func<IEnumerable<TConnectionProxy>, bool> _isConnectedPredicate = null;

        private bool _isConnected = false;


        // Constructors
        internal ConnectionProxyHostBase(IEnumerable<TConnectionProxy> connectionProxyCollection)
        {
            #region Contracts

            if (connectionProxyCollection == null) throw new ArgumentNullException();

            #endregion

            // ConnectionProxyCollection
            _connectionProxyCollection = connectionProxyCollection;

            // IsConnectedPredicate 
            _isConnectedPredicate = ConnectionProxyHost.CreateOneConnectedPredicate();
        }

        internal ConnectionProxyHostBase(IEnumerable<TConnectionProxy> connectionProxyCollection, Func<IEnumerable<ConnectionProxy>, bool> isConnectedPredicate)
        {
            #region Contracts

            if (connectionProxyCollection == null) throw new ArgumentNullException();
            if (isConnectedPredicate == null) throw new ArgumentNullException();

            #endregion

            // ConnectionProxyCollection
            _connectionProxyCollection = connectionProxyCollection;

            // IsConnectedPredicate 
            _isConnectedPredicate = isConnectedPredicate;
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
            // ConnectionProxyCollection
            foreach (TConnectionProxy connectionProxy in _connectionProxyCollection)
            {
                connectionProxy.Connected += this.Proxy_Connected;
                connectionProxy.Disconnected += this.Proxy_Disconnected;
                connectionProxy.Heartbeating += this.Proxy_Heartbeating;
                connectionProxy.Open();
            }
        }

        public void Close()
        {
            // ConnectionProxyCollection
            foreach (TConnectionProxy connectionProxy in _connectionProxyCollection)
            {
                connectionProxy.Close();
                connectionProxy.Connected -= this.Proxy_Connected;
                connectionProxy.Disconnected -= this.Proxy_Disconnected;
                connectionProxy.Heartbeating -= this.Proxy_Heartbeating;
            }
        }

        private void Refresh()
        {
            // IsConnected
            bool isConnected = _isConnectedPredicate(_connectionProxyCollection);

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


        public void Execute(Action<TConnectionProxy> executeDelegate)
        {
            #region Contracts

            if (executeDelegate == null) throw new ArgumentNullException();

            #endregion

            // ConnectionProxyCollection
            foreach (TConnectionProxy connectionProxy in _connectionProxyCollection)
            {
                try
                {
                    // Execute
                    executeDelegate(connectionProxy);

                    // Return
                    return;
                }
                catch
                {
                    // Nothing

                }
            }

            // Throw
            throw new ExecuteIgnoredException();
        }

        public TResult Execute<TResult>(Func<TConnectionProxy, TResult> executeDelegate)
        {
            #region Contracts

            if (executeDelegate == null) throw new ArgumentNullException();

            #endregion

            // ConnectionProxyCollection
            foreach (TConnectionProxy connectionProxy in _connectionProxyCollection)
            {
                try
                {
                    // Execute
                    TResult result = executeDelegate(connectionProxy);

                    // Return
                    return result;
                }
                catch
                {
                    // Nothing

                }
            }

            // Throw
            throw new ExecuteIgnoredException();
        }


        public void ExecuteAll(Action<TConnectionProxy> executeDelegate)
        {
            #region Contracts

            if (executeDelegate == null) throw new ArgumentNullException();

            #endregion

            // ConnectionProxyCollection
            foreach (TConnectionProxy connectionProxy in _connectionProxyCollection)
            {
                try
                {
                    // Execute
                    executeDelegate(connectionProxy);
                }
                catch
                {
                    // Throw
                    throw;
                }
            }
        }

        public IEnumerable<TResult> ExecuteAll<TResult>(Func<TConnectionProxy, TResult> executeDelegate)
        {
            #region Contracts

            if (executeDelegate == null) throw new ArgumentNullException();

            #endregion

            // Result
            List<TResult> resultCollection = new List<TResult>();

            // ConnectionProxyCollection
            foreach (TConnectionProxy connectionProxy in _connectionProxyCollection)
            {
                try
                {
                    // Execute
                    TResult result = executeDelegate(connectionProxy);

                    // Add
                    resultCollection.Add(result);
                }
                catch
                {
                    // Throw
                    throw;
                }
            }

            // Return
            return resultCollection;
        }


        // Handlers
        private void Proxy_Connected(object sender, EventArgs e)
        {
            // Refresh
            this.Refresh();
        }

        private void Proxy_Disconnected(object sender, EventArgs e)
        {
            // Refresh
            this.Refresh();
        }

        private void Proxy_Heartbeating(object sender, EventArgs e)
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

    public class ConnectionProxyHost<TService> : ConnectionProxyHostBase<ConnectionProxy<TService>>
        where TService : class, IConnectionService
    {
         // Constructors
        public ConnectionProxyHost(IEnumerable<ConnectionProxy<TService>> connectionProxyCollection) : base(connectionProxyCollection) { }

        public ConnectionProxyHost(IEnumerable<ConnectionProxy<TService>> connectionProxyCollection, Func<IEnumerable<ConnectionProxy>, bool> isConnectedPredicate) : base(connectionProxyCollection, isConnectedPredicate) { }
    }

    public class ConnectionProxyHost<TService, TCallback> : ConnectionProxyHostBase<ConnectionProxy<TService, TCallback>>
        where TService : class, IConnectionService
        where TCallback : class
    {
        // Constructors
        public ConnectionProxyHost(IEnumerable<ConnectionProxy<TService, TCallback>> connectionProxyCollection) : base(connectionProxyCollection) { }

        public ConnectionProxyHost(IEnumerable<ConnectionProxy<TService, TCallback>> connectionProxyCollection, Func<IEnumerable<ConnectionProxy>, bool> isConnectedPredicate) : base(connectionProxyCollection, isConnectedPredicate) { }
    }

    public static class ConnectionProxyHost
    {
        // ConnectedPredicate
        public static Func<IEnumerable<ConnectionProxy>, bool> CreateOneConnectedPredicate()
        {
            return delegate(IEnumerable<ConnectionProxy> connectionProxyCollection)
            {
                foreach (ConnectionProxy connectionProxy in connectionProxyCollection)
                {
                    if (connectionProxy.IsConnected == true)
                    {
                        return true;
                    }
                }
                return false;
            };
        }

        public static Func<IEnumerable<ConnectionProxy>, bool> CreateAllConnectedPredicate()
        {
            return delegate(IEnumerable<ConnectionProxy> connectionProxyCollection)
            {
                foreach (ConnectionProxy connectionProxy in connectionProxyCollection)
                {
                    if (connectionProxy.IsConnected == false)
                    {
                        return false;
                    }
                }
                return true;
            };
        }
    }
}