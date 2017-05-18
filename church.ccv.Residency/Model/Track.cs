﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using church.ccv.Residency.Data;

namespace church.ccv.Residency.Model
{
    /// <summary>
    /// 
    /// </summary>
    [Table( "_church_ccv_Residency_Track" )]
    [DataContract]
    public class Track : NamedModel<Track>
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the residency period id.
        /// </summary>
        /// <value>
        /// The residency period id.
        /// </value>
        [Required]
        [DataMember]
        public int PeriodId { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        /// <value>
        /// The display order.
        /// </value>
        [Required]
        [DataMember]
        public int DisplayOrder { get; set; }

        #endregion

        #region Virtual Properties

        /// <summary>
        /// Gets or sets the residency period.
        /// </summary>
        /// <value>
        /// The residency period.
        /// </value>
        public virtual Period Period { get; set; }

        /// <summary>
        /// Gets or sets the residency competencies.
        /// </summary>
        /// <value>
        /// The residency competencies.
        /// </value>
        public virtual List<Competency> Competencies { get; set; }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public partial class TrackConfiguration : EntityTypeConfiguration<Track>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackConfiguration"/> class.
        /// </summary>
        public TrackConfiguration()
        {
            this.HasRequired( p => p.Period ).WithMany( p => p.Tracks ).HasForeignKey( p => p.PeriodId ).WillCascadeOnDelete( false );
        }
    }
}