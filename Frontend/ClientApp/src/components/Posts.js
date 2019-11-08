import React, { Component } from 'react';
import Row from "react-bootstrap/Row";
import Masonry from 'react-masonry-infinite';
import Utils from "../Utils";
import './Posts.css';

export class Posts extends Component {
  displayName = Posts.name

  constructor(props) {
    super(props);
    this.state = { posts: [], loading: true, hasMore: false };
    this.isFetching = false;

    this.loadMore = this.loadMore.bind(this);
    this.imageReady = this.imageReady.bind(this);

    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + props.match.params.blogname)
      .then(response => response.json())
      .then(data => {
        this.setState({ posts: data, loading: false, hasMore: data.length === 20 });
      });
  }
  
  loadMore() {
    if (this.isFetching) {
      return;
    }

    this.isFetching = true;
    const [lastPost] = this.state.posts.slice(-1);
    fetch(process.env.REACT_APP_API_ROOT + '/api/posts/' + this.props.match.params.blogname + "?after=" + lastPost.Id)
      .then(response => response.json())
      .then(data => {
        this.isFetching = false;
        this.setState(state => ({
          posts: state.posts.concat(data),
          hasMore: data.length === 20 
        }));
      });
  }

  imageReady() {
    this.masonryGrid.forcePack();
  }

  renderPostsTable(posts) {
    return (
      <Masonry className="masonry" hasMore={this.state.hasMore} loadMore={this.loadMore} ref={(child) => { this.masonryGrid = child; }}
        sizes={[
          { columns: 1, gutter: 10 },
          { mq: '550px', columns: 2, gutter: 10 },
          { mq: '810px', columns: 3, gutter: 10 },
          { mq: '1070px', columns: 4, gutter: 10 },
          { mq: '1330px', columns: 5, gutter: 10 },
          { mq: '1590px', columns: 6, gutter: 10 },
          { mq: '1850px', columns: 7, gutter: 10 },
        ]}
      >
        {posts.map(post =>
          <div key={post.Id}>
            {((!post.Photos || post.Photos.length === 0) && (!post.Videos || post.Videos.length === 0)) && 
              <div width="250">
                <a href={"/#/post/" + post.Blogname + "/" + post.Id}>No photo</a>
              </div>
            }
            {(post.Photos && post.Photos.length !== 0) &&
              <div className="photo-post"><a href={ "/#/post/" + post.Blogname + "/" + post.Id}> 
                <img src={Utils.GetSmallPhotoUrl(post)} width="250" data-id={post.Id} onLoad={this.imageReady} onError={this.imageReady} alt="" />
              </a></div>
            }
            {(post.Videos && post.Videos.length !== 0) &&
              <div className="video-post"><a href={"/#/post/" + post.Blogname + "/" + post.Id}>
                <img src={post.Videos[0].ThumbUrl} width="250" data-id={post.Id} onLoad={this.imageReady} onError={this.imageReady} alt="" />
                <img src={require('./images/video_play.png')} class="video-icon-overlay" alt="" />
              </a></div>
            }
          </div>
        )}
      </Masonry>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : this.renderPostsTable(this.state.posts);

    return (
      <Row>
        <div class='col'>
          <h1>{this.props.match.params.blogname}</h1>
          {contents}
        </div>
      </Row>
    );
  }
}
