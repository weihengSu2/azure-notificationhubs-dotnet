﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.Serialization;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.Azure.NotificationHubs.Common;

namespace Microsoft.Azure.NotificationHubs
{
    /// <summary>
    ///   Metadata description of the NotificationHub.
    /// </summary>
    [DataContract(Name = ManagementStrings.NotificationHubDescription, Namespace = ManagementStrings.Namespace)]
    public sealed class NotificationHubDescription : EntityDescription
    {
        string _path;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="path">path of the NotificationHub.</param>
        public NotificationHubDescription(string path)
        {
            Path = path;
        }

        /// <summary>
        ///   Gets the full path of the notificationHub.
        /// </summary>
        /// <remarks>
        ///   This is a relative path to the <see cref = "NamespaceManager._baseUri" />.
        /// </remarks>
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                ThrowIfReadOnly();

                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException(SRCore.ArgumentNullOrEmpty("Path"));
                }

                if (value.Length > Constants.NotificationHubNameMaximumLength)
                {
                    throw new ArgumentException(
                        string.Format(SRClient.NotificationHubNameLengthTooLong, Constants.NotificationHubNameMaximumLength));
                }

                _path = value;
            }
        }

        /// <summary>
        /// Sets the access passwords.
        /// </summary>
        /// <param name="fullAccessRuleName">name of the full access rule.</param>
        /// <param name="fullAccessPassword">The full access password.</param>
        /// <param name="listenAccessRuleName">Name of the listen access rule.</param>
        /// <param name="listenAccessPassword">The listen access password.</param>
        /// <exception cref="ArgumentNullException">
        /// fullAccessRuleName
        /// or
        /// fullAccessPassword
        /// or
        /// listenAccessRuleName
        /// or
        /// listenAccessPassword
        /// </exception>
        public void SetAccessPasswords(string fullAccessRuleName, string fullAccessPassword, string listenAccessRuleName, string listenAccessPassword)
        {
            if (string.IsNullOrWhiteSpace(fullAccessRuleName))
            {
                throw new ArgumentNullException(nameof(fullAccessRuleName));
            }

            if (string.IsNullOrWhiteSpace(fullAccessPassword))
            {
                throw new ArgumentNullException(nameof(fullAccessPassword));
            }

            if (string.IsNullOrWhiteSpace(listenAccessRuleName))
            {
                throw new ArgumentNullException(nameof(listenAccessRuleName));
            }

            if (string.IsNullOrWhiteSpace(listenAccessPassword))
            {
                throw new ArgumentNullException(nameof(listenAccessPassword));
            }

            SetAccessPassword(fullAccessRuleName, fullAccessPassword, new AccessRights[] { AccessRights.Listen, AccessRights.Send, AccessRights.Manage });
            SetAccessPassword(listenAccessRuleName, listenAccessPassword, new AccessRights[] { AccessRights.Listen });
        }

        /// <summary>
        /// Sets the access password.
        /// </summary>
        public void SetAccessPassword(string accessKeyName, string password, IEnumerable<AccessRights> rights)
        {
            lock (Authorization)
            {
                SharedAccessAuthorizationRule rule;
                if (Authorization.TryGetSharedAccessAuthorizationRule(accessKeyName, out rule))
                {
                    rule.PrimaryKey = password;
                    rule.Rights = rights;
                }
                else
                {
                    rule = new SharedAccessAuthorizationRule(
                                accessKeyName,
                                password,
                                rights);
                    Authorization.Add(rule);
                }
            }
        }

        /// <summary>
        /// Gets the authorization rules.
        /// </summary>
        /// <value>
        /// The authorization.
        /// </value>
        public AuthorizationRules Authorization
        {
            get
            {
                if (InternalAuthorization == null)
                {
                    InternalAuthorization = new AuthorizationRules();
                }

                return InternalAuthorization;
            }
        }

        /// <summary>
        /// Gets or sets the expiration time of all registrations in this Notificationhub.
        /// </summary>
        /// 
        /// <returns>
        /// The registration TTL.
        /// </returns>
        public TimeSpan? RegistrationTtl
        {
            get
            {
                if (InternalRegistrationTtl == null)
                {
                    InternalRegistrationTtl = Constants.DefaultRegistrationTtl;
                }

                return InternalRegistrationTtl;
            }
            set
            {
                ThrowIfReadOnly();

                if (value.Value < Constants.MinimumRegistrationTtl)
                {
                    throw new ArgumentOutOfRangeException("value", value, string.Format(CultureInfo.InvariantCulture, "Registration Ttl must be at least {0}", Constants.MinimumRegistrationTtl));
                }


                InternalRegistrationTtl = value;
            }
        }

        private bool? internalStatus = null;

        /// <summary>
        /// Gets or sets the internal status string which indicates if this
        /// notification hub is disabled or not. If this value is
        /// <see langword="true"/> then the notification hub is disabled; otherwise it
        /// is enabled. When disabled all runtime operations (i.e. registration
        /// management and sends) will return HTTP status code 403 
        /// <see cref="HttpStatusCode.Forbidden"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the property that is persisted through serialization.
        /// </para>
        /// <para>
        /// Multi-tenant applications are applications that have to push
        /// notifications to a single mobile application on behalf of multiple
        /// parties (or tenants). One pattern to achieve this goal, which
        /// achieves user isolation between tenants, is to create one hub per
        /// tenant and store application credentials at the namespace level. In
        /// these cases though, it might be required for the multi-tenant
        /// application to disable the notification hub of a particular tenant
        /// in order to avoid abuse (either resulting in service degradation
        /// for other tenants, or in extra charges for the multi-tenant
        /// application owner).
        /// </para>
        /// </remarks>
        /// <value>
        /// The internal status string. If this value is <see langword="true"/> then
        /// the notification hub is disabled; otherwise it is enabled.
        /// </value>
        /// <seealso cref="InternalStatus"/>
        /// <seealso cref="HttpStatusCode"/>
        [DataMember(Name = ManagementStrings.Status, IsRequired = false, EmitDefaultValue = false, Order = 1016)]
        private bool? InternalStatus
        {
            get { return internalStatus; }
            set { internalStatus = value; }
        }

        /// <summary>
        /// Gets of sets a value indicating if this notification hub is
        /// disabled. When <see langword="true" /> all runtime operations (i.e.
        /// registration management and sends) will return HTTP status code 403 
        /// <see cref="HttpStatusCode.Forbidden"/>.
        /// </summary>
        /// <remarks>
        /// Multi-tenant applications are applications that have to push
        /// notifications to a single mobile application on behalf of multiple
        /// parties (or tenants). One pattern to achieve this goal, which
        /// achieves user isolation between tenants, is to create one hub per
        /// tenant and store application credentials at the namespace level. In
        /// these cases though, it might be required for the multi-tenant
        /// application to disable the notification hub of a particular tenant
        /// in order to avoid abuse (either resulting in service degradation
        /// for other tenants, or in extra charges for the multi-tenant
        /// application owner).
        /// </remarks>
        /// <value>
        /// <see langword="true" /> if this notification hub is disabled;
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <seealso cref="InternalStatus"/>
        /// <seealso cref="HttpStatusCode"/>
        [IgnoreDataMember]
        public bool IsDisabled
        {
            get { return internalStatus ?? false; }
            set { internalStatus = value; }
        }

        /// <summary>
        /// If true, the entity can be accessed by anonymous users
        /// </summary>
        /// 
        /// <returns>
        /// true if the description is anonymously accessible; otherwise, false.
        /// </returns>
        public bool IsAnonymousAccessible
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the APNS credential.
        /// </summary>
        /// 
        /// <returns>
        /// The APNS credential.
        /// </returns>
        [DataMember(Name = ManagementStrings.ApnsCredential, IsRequired = false, EmitDefaultValue = false, Order = 1001)]
        public ApnsCredential ApnsCredential
        {
            get;
            set;
        }

        [DataMember(Name = ManagementStrings.RegistrationTtl, IsRequired = false, EmitDefaultValue = false, Order = 1002)]
        internal TimeSpan? InternalRegistrationTtl { get; set; }

        /// <summary>
        /// Gets or sets the WNS credential.
        /// </summary>
        /// 
        /// <returns>
        /// The WNS credential.
        /// </returns>
        [DataMember(Name = ManagementStrings.WnsCredential, IsRequired = false, EmitDefaultValue = false, Order = 1003)]
        public WnsCredential WnsCredential
        {
            get;
            set;
        }

        [DataMember(Name = ManagementStrings.AuthorizationRules, IsRequired = false, Order = 1004,
            EmitDefaultValue = false)]
        internal AuthorizationRules InternalAuthorization { get; set; }

        /// <summary>
        /// Gets or sets the FCM legacy credential.
        /// </summary>
        /// 
        /// <returns>
        /// The FCM legacy credential.
        /// </returns>
        [DataMember(Name = ManagementStrings.GcmCredential, IsRequired = false, EmitDefaultValue = false, Order = 1005)]
        public FcmCredential FcmCredential
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the MPNS credential. An <see cref="T:Microsoft.Azure.NotificationHubs.MpnsCredential"/> instance with no defined certificate enables MPNS unauthenticated MPNS support.
        /// </summary>
        /// 
        /// <returns>
        /// The MPNS credential.
        /// </returns>
        [DataMember(Name = ManagementStrings.MpnsCredential, IsRequired = false, EmitDefaultValue = false, Order = 1006)]
        public MpnsCredential MpnsCredential
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the daily operations for the Notificationhub.
        /// </summary>
        /// 
        /// <returns>
        /// The daily operations for the Notificationhub.
        /// </returns>
        [DataMember(Name = ManagementStrings.DailyOperations, IsRequired = false, EmitDefaultValue = false, Order = 1007)]
        public long DailyOperations
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the daily maximum active devices for Notificationhub.
        /// </summary>
        /// 
        /// <returns>
        /// The daily maximum active devices for Notificationhub.
        /// </returns>
        [DataMember(Name = ManagementStrings.DailyMaxActiveDevices, IsRequired = false, EmitDefaultValue = false, Order = 1008)]
        public long DailyMaxActiveDevices
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the daily maximum active registrations for the Notificationhub.
        /// </summary>
        /// 
        /// <returns>
        /// The daily maximum active registrations for the Notificationhub.
        /// </returns>
        [DataMember(Name = ManagementStrings.DailyMaxActiveRegistrations, IsRequired = false, EmitDefaultValue = false, Order = 1009)]
        public long DailyMaxActiveRegistrations
        {
            get;
            internal set;
        }


        [DataMember(Name = ManagementStrings.UserMetadata, IsRequired = false, EmitDefaultValue = false, Order = 1010)]
        internal string InternalUserMetadata { get; set; }

        /// <summary>
        /// Gets or sets the Adm credential credential.
        /// </summary>
        /// 
        /// <returns>
        /// The Adm credential credential.
        /// </returns>
        [DataMember(Name = ManagementStrings.AdmCredential, IsRequired = false, EmitDefaultValue = false, Order = 1014)]
        public AdmCredential AdmCredential
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Baidu credential.
        /// </summary>
        /// <value>
        /// The Baidu credential.
        /// </value>
        [DataMember(Name = ManagementStrings.BaiduCredential, IsRequired = false, EmitDefaultValue = false, Order = 1016)]
        public BaiduCredential BaiduCredential
        {
            get;
            set;
        }

        /// <summary>
        ///   Gets/Sets any User Metadata associated with the NotificationHub.
        /// </summary>
        /// 
        /// <returns>
        /// The user metadata associated with the NotificationHub.
        /// </returns>
        public string UserMetadata
        {
            get
            {
                return this.InternalUserMetadata;
            }
            set
            {
                this.ThrowIfReadOnly();
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.InternalUserMetadata = null;
                    return;
                }

                if (value.Length > Constants.MaximumUserMetadataLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(UserMetadata));
                }
                this.InternalUserMetadata = value;
            }
        }

        internal override bool RequiresEncryption
        {
            get { return true; }
        }
    }
}
