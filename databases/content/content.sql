CREATE TABLE public.content
(
    id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    created timestamp with time zone NOT NULL,
    release timestamp with time zone NOT NULL,
    publishing_id character varying(64) NOT NULL,
    content bytea NOT NULL,
    content_type_name text NOT NULL,
    type text NOT NULL,
    CONSTRAINT pk_content PRIMARY KEY (id)
);

CREATE INDEX ix_content_content_type_name
    ON public.content USING btree
    (content_type_name ASC NULLS LAST);

CREATE INDEX ix_content_publishing_id
    ON public.content USING btree
    (publishing_id ASC NULLS LAST);

CREATE INDEX ix_content_release
    ON public.content USING btree
    (release ASC NULLS LAST);

CREATE INDEX ix_content_type
    ON public.content USING btree
    (type ASC NULLS LAST);

CREATE UNIQUE INDEX ix_content_publishing_id_type
    ON public.content USING btree
    (publishing_id ASC NULLS LAST, type ASC NULLS LAST)
    WHERE publishing_id IS NOT NULL
    AND type = 'ExposureKeySet'::text;
