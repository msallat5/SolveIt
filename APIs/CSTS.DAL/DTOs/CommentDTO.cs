﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSTS.DAL.DTOs
{
    public class CreateCommentDTO
    {
        public string Content { get; set; }
        public Guid UserId { get; set; }
        public Guid TicketId { get; set; }
    }

    public class CommentResponseDTO
    {
        public Guid CommentId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UserName { get; set; }

        public class UpdateCommentDTO
        {
            public string Content { get; set; }
        }

        public class CommentSummaryDTO
        {
            public Guid CommentId { get; set; }
            public string Content { get; set; }
        }
    }
}
