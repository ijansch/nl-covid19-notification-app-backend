// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Mime;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.Entities
{
    [Table("Content")]
    public class ContentEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Release { get; set; }

        [MaxLength(64)]
        public string PublishingId { get; set; }
        public byte[] Content { get; set; }

        public string ContentTypeName { get; set; } = MediaTypeNames.Application.Zip;
        public ContentTypes Type { get; set; }
    }
}
